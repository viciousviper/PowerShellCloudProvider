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
using System.Composition.Hosting.Core;

namespace IgorSoft.PowerShellCloudProvider.Tests.CompositionExtensions
{
    internal class DelegateExportDescriptorProvider : SinglePartExportDescriptorProvider
    {
        private readonly CompositeActivator activator;

        public DelegateExportDescriptorProvider(Func<object> exportedInstanceFactory, Type contractType, string contractName, IDictionary<string, object> metadata, bool isShared) : base(contractType, contractName, metadata)
        {
            if (exportedInstanceFactory == null)
                throw new ArgumentNullException(nameof(exportedInstanceFactory));

            // Runs the factory method, validates the result and registers it for disposal if necessary.
            CompositeActivator constructor = (c, o) => {
                var result = exportedInstanceFactory();
                if (result == null)
                    throw new InvalidOperationException("Delegate factory returned null.");

                var disposableResult = result as IDisposable;
                if (disposableResult != null)
                    c.AddBoundInstance(disposableResult);

                return result;
            };

            if (isShared) {
                var sharingId = LifetimeContext.AllocateSharingId();
                activator = (c, o) => {
                    // Find the root composition scope.
                    var scope = c.FindContextWithin(null);
                    if (scope == c) {
                        // We're already in the root scope, create the instance
                        return scope.GetOrCreate(sharingId, o, constructor);
                    } else {
                        // Composition is moving up the hierarchy of scopes; run
                        // a new operation in the root scope.
                        return CompositionOperation.Run(scope, (c1, o1) => c1.GetOrCreate(sharingId, o1, constructor));
                    }
                };
            } else {
                activator = constructor;
            }
        }

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
        {
            if (IsSupportedContract(contract))
                yield return new ExportDescriptorPromise(contract, "factory delegate", true, NoDependencies, _ => ExportDescriptor.Create(activator, Metadata));
        }
    }
}