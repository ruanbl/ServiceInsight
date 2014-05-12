﻿namespace Particular.ServiceInsight.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Caliburn.PresentationFramework.Screens;
    using Desktop.Core;
    using Desktop.Explorer.QueueExplorer;
    using Desktop.Models;
    using Helpers;
    using NSubstitute;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class QueueCreationViewModel
    {
        Desktop.Shell.QueueCreationViewModel Model;
        IQueueManagerAsync QueueManager;
        IQueueExplorerViewModel Explorer;
        INetworkOperations Network;

        [SetUp]
        public void TestInitialize()
        {
            IList<string> machines = new List<string> { Environment.MachineName, "AnotherMachine" };
            QueueManager = Substitute.For<IQueueManagerAsync>();
            Explorer = Substitute.For<IQueueExplorerViewModel>();
            Network = Substitute.For<INetworkOperations>();
            Model = new Desktop.Shell.QueueCreationViewModel(QueueManager, Explorer, Network);
            Network.GetMachines().Returns(Task.FromResult(machines));
        }

        [Test]
        public void should_select_correct_default_values_when_displayed()
        {
            Model.IsTransactional.ShouldBe(true);
            Model.SelectedMachine.ShouldBe(null);
        }

        [Test]
        public void should_display_network_machine_names_when_retrieval_is_finished()
        {
            AsyncHelper.Run(() => ((IActivate)Model).Activate());

            Model.Machines.Count.ShouldBe(2);
            Model.WorkInProgress.ShouldBe(false);
        }

        [Test]
        public void should_allow_creating_a_queue()
        {
            Model.SelectedMachine = Environment.MachineName;
            Model.QueueName = "TestQueue";

            Model.CanAccept().ShouldBe(true);
        }

        [Test]
        public void should_request_the_queue_to_be_created_when_dialog_is_closed()
        {
            const string queueName = "TestQueue";
            var parent = new TestConductorScreen();
            var addedQueue = new Queue(Environment.MachineName, queueName);
            QueueManager.CreatePrivateQueue(Arg.Any<Queue>()).ReturnsForAnyArgs(addedQueue);

            Model.Parent = parent;
            Model.SelectedMachine = Environment.MachineName;
            Model.QueueName = queueName;
            Model.IsTransactional = true;
            Model.Accept();

            var expected = Address.Parse(queueName + "@.");
            QueueManager.Received().CreatePrivateQueueAsync(Arg.Is<Queue>(q => q.Address.Equals(expected)), Arg.Is(true));
            Model.IsActive.ShouldBe(false);
        }
    }
}