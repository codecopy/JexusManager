﻿// Copyright (c) Lex Li. All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using JexusManager.Features.Authorization;
using Microsoft.Web.Management.Client.Extensions;

namespace Tests.Authentication
{
    using System;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using global::JexusManager.Features.Authentication;
    using global::JexusManager.Services;

    using Microsoft.Web.Administration;
    using Microsoft.Web.Management.Client;
    using Microsoft.Web.Management.Client.Win32;
    using Microsoft.Web.Management.Server;

    using Moq;

    using Xunit;
    using System.Xml.Linq;
    using System.Xml.XPath;

    public class FormsAuthenticationFeatureServerTestFixture
    {
        private FormsAuthenticationFeature _feature;

        private ServerManager _server;

        private ServiceContainer _serviceContainer;

        private const string Current = @"applicationHost.config";

        public void SetUp()
        {
            const string Original = @"original.config";
            const string OriginalMono = @"original.mono.config";
            if (Helper.IsRunningOnMono())
            {
                File.Copy("Website1/original.config", "Website1/web.config", true);
                File.Copy(OriginalMono, Current, true);
            }
            else
            {
                File.Copy("Website1\\original.config", "Website1\\web.config", true);
                File.Copy(Original, Current, true);
            }

            Environment.SetEnvironmentVariable(
                "JEXUS_TEST_HOME",
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            _server = new IisExpressServerManager(Current);

            _serviceContainer = new ServiceContainer();
            _serviceContainer.RemoveService(typeof(IConfigurationService));
            _serviceContainer.RemoveService(typeof(IControlPanel));
            var scope = ManagementScope.Server;
            _serviceContainer.AddService(typeof(IControlPanel), new ControlPanel());
            _serviceContainer.AddService(typeof(IConfigurationService),
                new ConfigurationService(null, _server.GetApplicationHostConfiguration(), scope, _server, null, null, null, null, null));

            _serviceContainer.RemoveService(typeof(IManagementUIService));
            var mock = new Mock<IManagementUIService>();
            mock.Setup(
                action =>
                    action.ShowMessage(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<MessageBoxButtons>(),
                        It.IsAny<MessageBoxIcon>(),
                        It.IsAny<MessageBoxDefaultButton>())).Returns(DialogResult.Yes);
            _serviceContainer.AddService(typeof(IManagementUIService), mock.Object);

            var module = new AuthenticationModule();
            module.TestInitialize(_serviceContainer, null);

            _feature = new FormsAuthenticationFeature(module);
            _feature.Load();
        }

        [Fact]
        public void TestBasic()
        {
            SetUp();
            Assert.False(_feature.IsEnabled);

            const string Expected = @"expected_remove.config";
            var document = XDocument.Load(Current);
            document.Save(Expected);

            try
            {
                _feature.Enable();
                Assert.True(_feature.IsEnabled);
                XmlAssert.Equal(Expected, Current);

                _feature.Disable();
                Assert.False(_feature.IsEnabled);
                XmlAssert.Equal(Expected, Current);
            }
            catch (Exception ex)
            {
                // If not admin, this exception is expected.
                Assert.IsType<UnauthorizedAccessException>(ex);
            }
        }
    }
}