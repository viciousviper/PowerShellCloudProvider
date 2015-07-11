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
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IgorSoft.PowerShellCloudProvider.Tests.Progress
{
    [TestClass]
    public class ProgressProxyTests
    {
        [TestMethod]
        public void ProgressProxy_WhereFuncReturnsResult_ReportsProgressOnCurrentThread()
        {
            const string activity = "ACTIVITY";
            const string target = "TARGET";
            const int percentComplete = 50;
            const int bytesTransferred = 512;
            const int bytesTotal = 1024;
            const int result = 42;

            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            var providerContextMock = new Mock<IProviderContext>(MockBehavior.Strict);
            providerContextMock.Setup(p => p.WriteProgress(It.Is<ProgressRecord>(r => r.Activity == activity && r.StatusDescription == target && r.PercentComplete == percentComplete
                            && r.CurrentOperation == "Transferring 512b/1Kb" && r.RecordType == ProgressRecordType.Processing
                            && Thread.CurrentThread.ManagedThreadId == currentThreadId)
            ));

            var sut = new ProgressProxy(providerContextMock.Object, activity, target);

            ProgressProxy.ProgressFunc<int> func = async p => {
                return await Task.Run(() => {
                    Assert.AreNotEqual(currentThreadId, Thread.CurrentThread.ManagedThreadId, "Current Thread not expected for func evaluation");
                    p.Report(new ProgressValue(percentComplete, bytesTransferred, bytesTotal));
                    return result;
                });
            };
            Assert.AreEqual(result, ProgressProxy.TraceProgressOn(func, sut), "Unexpected result");

            providerContextMock.VerifyAll();
        }
    }
}
