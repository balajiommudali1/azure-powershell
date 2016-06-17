﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace StaticAnalysis.SignatureVerifier
{
    public class SignatureVerifier : IStaticAnalyzer
    {
        const int ForceWithoutShouldProcessAttribute = 8000;
        const int ConfirmLeveleWithNoShouldProcess = 8010;
        const int ActionIndicatesShouldProcess = 8100;
        const int ConfirmLevelChange = 8200;
        const int CmdletWithDestructiveVerbNoForce = 8210;
        const int CmdletWithDestructiveVerb = 98300;
        const int CmdletWithForceParameter = 98310;
        public SignatureVerifier()
        {
            Name = "Signature Verifier";
        }
        public AnalysisLogger Logger { get; set; }
        public string Name { get; private set; }

        private AppDomain _appDomain;

        /// <summary>
        /// Given a set of directory paths containing PowerShell module folders, analyze the help 
        /// in the module folders and report any issues
        /// </summary>
        /// <param name="scopes"></param>
        public void Analyze(IEnumerable<string> scopes)
        {
            var savedDirectory = Directory.GetCurrentDirectory();
            var processedHelpFiles = new List<string>();
            var issueLogger = Logger.CreateLogger<SignatureIssue>("SignatureIssues.csv");
            foreach (var baseDirectory in scopes.Where(s => !s.Contains("ServiceManagement") && Directory.Exists(Path.GetFullPath(s))))
            {
                foreach (var directory in Directory.EnumerateDirectories(Path.GetFullPath(baseDirectory)))
                {
                    var helpFiles = Directory.EnumerateFiles(directory, "*.dll-Help.xml")
                        .Where(f => !processedHelpFiles.Contains(Path.GetFileName(f), 
                            StringComparer.OrdinalIgnoreCase)).ToList();
                    if (helpFiles.Any())
                    {
                        Directory.SetCurrentDirectory(directory);
                        foreach (var helpFile in helpFiles)
                        {
                           var cmdletFile = helpFile.Substring(0, helpFile.Length - "-Help.xml".Length);
                            var helpFileName = Path.GetFileName(helpFile);
                            var cmdletFileName = Path.GetFileName(cmdletFile);
                            if (File.Exists(cmdletFile) )
                            {
                                issueLogger.Decorator.AddDecorator(a => a.AssemblyFileName = cmdletFileName, "AssemblyFileName");
                                processedHelpFiles.Add(helpFileName);
                                var proxy = EnvironmentHelpers.CreateProxy<CmdletSignatureLoader>(directory, out _appDomain);
                                var cmdlets = proxy.GetCmdlets(cmdletFile);
                                foreach (var cmdlet in cmdlets)
                                {
                                    string description = null;
                                    int problemId = 0;
                                    string remediation = "Determine if the cmdlet should implement ShouldProcess, and " +
                                                          "if so, determine if it should implement Force / ShouldContinue";
                                    
                                    int severity = int.MaxValue;

                                    if (!cmdlet.SupportsShouldProcess && cmdlet.HasForceSwitch)
                                    {
                                        problemId = ForceWithoutShouldProcessAttribute;
                                        description = string.Format("{0} Has  -Force parameter but does not set the SupportsShouldProcess " +
                                                                    "property to true in the Cmdlet attribute.", cmdlet.Name);
                                    }

                                    if (!cmdlet.SupportsShouldProcess && cmdlet.ConfirmImpact != ConfirmImpact.Medium)
                                    {
                                        problemId = ConfirmLeveleWithNoShouldProcess;
                                        description =
                                            string.Format("{0} Changes the ConfirmImpact but does not set the " +
                                                          "SupportsShouldProcess property to true in the cmdlet attribute.",
                                                cmdlet.Name);

                                    }

                                    if (!cmdlet.SupportsShouldProcess && cmdlet.IsShouldProcessVerb)
                                    {
                                        problemId = ActionIndicatesShouldProcess;
                                        description =
                                            string.Format(
                                                "{0} Does not support ShouldProcess, but the cmdlet verb {1} indicates that it should.",
                                                cmdlet.Name, cmdlet.VerbName);

                                    }

                                    if (cmdlet.ConfirmImpact != ConfirmImpact.Medium)
                                    {
                                        problemId = ConfirmLevelChange;
                                        description = string.Format("{0} changes the confirm impact.  Please ensure that the " +
                                                                    "change in ConfirmImpact is justified", cmdlet.Name);
                                        remediation =
                                            "Verify that ConfirmImpact is changed appropriately by the cmdlet. " +
                                            "It is very rare for a cmdlet to change the ConfirmImpact.";
                                    }

                                    if (cmdlet.IsShouldContinueVerb && !cmdlet.HasForceSwitch)
                                    {
                                        problemId = CmdletWithDestructiveVerbNoForce;
                                        description =
                                            string.Format(
                                                "{0} does not have a Force parameter, but the cmdlet verb '{1}' " +
                                                "indicates that it may perform destrucvie actions under certain " +
                                                "circumstances. Consider wehtehr the cmdlet should have a Force " +
                                                "parameter anduse ShouldContinue under some circumstances. ",
                                                cmdlet.Name, cmdlet.VerbName);
                                        remediation = "Consider wehtehr the cmdlet should have a Force " +
                                                      "parameter and use ShouldContinue under some circumstances. ";

                                    }

                                    // Temporary detections, please remove these before checking in

                                    if (cmdlet.IsShouldContinueVerb)
                                    {
                                        problemId = CmdletWithDestructiveVerb;
                                        description = string.Format(
                                            "[Temporary]: {0} uses a destructive verb.  Check to see if the cmdlet " +
                                            "implements Confirmation correctly.", cmdlet.Name);
                                    }

                                    if (cmdlet.HasForceSwitch)
                                    {
                                        problemId = CmdletWithForceParameter;
                                        description = string.Format(
                                            "[Temporary]: {0} has a Force switch.  Check to see if the cmdlet " +
                                            "implements Confirmation correctly.", cmdlet.Name);
                                    }


                                    if (problemId > 0)
                                    {
                                        issueLogger.LogRecord(new SignatureIssue
                                        {
                                            ClassName = cmdlet.ClassName,
                                            Target = cmdlet.Name,
                                            Description = description,
                                            Remediation = remediation,
                                            Severity = severity
                                        });
                                    }
                                }

                                AppDomain.Unload(_appDomain);
                                issueLogger.Decorator.Remove("AssemblyFileName");
                            }
                        }

                        Directory.SetCurrentDirectory(savedDirectory);
                    }
                }
            }
        }

    }
}