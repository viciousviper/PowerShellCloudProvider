/*
The MIT License(MIT)

Copyright(c) 2015 IgorSoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.PowerShellCloudProvider.Tests.Fixtures
{
    internal class PipelineFixture
    {
        private readonly Runspace runspace;

        public PipelineFixture()
        {
            var runspaceConfiguration = RunspaceConfiguration.Create();
            runspaceConfiguration.Providers.Append(new ProviderConfigurationEntry(CloudProvider.PROVIDER_NAME, typeof(CloudProvider), null));

            runspace = RunspaceFactory.CreateRunspace(runspaceConfiguration);
            runspace.Open();
        }

        public static PSCredential GetCredential(string userName, string password)
        {
            var securePassword = new System.Security.SecureString();
            foreach (var c in password)
                securePassword.AppendChar(c);

            return new PSCredential(userName, securePassword);
        }

        public void SetVariable(string name, object value)
        {
            runspace.SessionStateProxy.SetVariable(name, value);
        }

        public IList<PSObject> Invoke(params string[] commands)
        {
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));

            var pipeline = default(Pipeline);
            if (commands.Length == 1) {
                pipeline = runspace.CreatePipeline(commands[0]);
            } else if (commands.Length > 1) {
                pipeline = runspace.CreatePipeline();
                foreach (var command in commands)
                    pipeline.Commands.AddScript(command);
            }
            var result = pipeline.Invoke();

            Assert.IsFalse(pipeline.HadErrors, string.Join(", ", pipeline.Error.ReadToEnd().Select(e => ((ErrorRecord)((PSObject)e).BaseObject).CategoryInfo.ToString())));
 
            return result;
        }
    }
}