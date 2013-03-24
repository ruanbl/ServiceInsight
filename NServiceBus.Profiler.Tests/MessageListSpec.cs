﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Caliburn.PresentationFramework.ApplicationModel;
using Machine.Specifications;
using NServiceBus.Profiler.Common.Models;
using NServiceBus.Profiler.Core;
using NServiceBus.Profiler.Core.Management;
using NServiceBus.Profiler.Desktop.Events;
using NServiceBus.Profiler.Desktop.Explorer.EndpointExplorer;
using NServiceBus.Profiler.Desktop.Explorer.QueueExplorer;
using NServiceBus.Profiler.Desktop.MessageList;
using NServiceBus.Profiler.Desktop.ScreenManager;
using NServiceBus.Profiler.Desktop.Search;
using NServiceBus.Profiler.Tests.Helpers;
using NSubstitute;

namespace NServiceBus.Profiler.Tests
{
    [Subject("message list")]
    public abstract class with_the_message_list
    {
        protected static IQueueManagerAsync QueueManager;
        protected static IWindowManagerEx WindowManager;
        protected static IEventAggregator EventAggregator;
        protected static IManagementService ManagementService;
        protected static IEndpointConnectionProvider EndpointConnectionProvider;
        protected static ISearchBarViewModel SearchBar;
        protected static Dictionary<Queue, List<MessageInfo>> MessageStore;
        protected static MessageListViewModel MessageList;
        
        Establish context = () =>
        {
            EventAggregator = Substitute.For<IEventAggregator>();
            ManagementService = Substitute.For<IManagementService>();
            EndpointConnectionProvider = Substitute.For<IEndpointConnectionProvider>();
            MessageStore = new Dictionary<Queue, List<MessageInfo>>();
            QueueManager = new FakeQueueManager(MessageStore);
            WindowManager = Substitute.For<IWindowManagerEx>();
            SearchBar = Substitute.For<ISearchBarViewModel>();
            MessageList = new MessageListViewModel(EventAggregator, WindowManager, ManagementService, QueueManager, EndpointConnectionProvider, SearchBar);
        };
    }

    public class loads_the_messages_from_the_endpoint : with_the_message_list
    {
        protected static Endpoint Endpoint;

        Establish context = () =>
        {
            Endpoint = new Endpoint { Machine = "localhost", Name = "Service" };
            ManagementService.GetAuditMessages(Arg.Any<string>(), Arg.Is(Endpoint), Arg.Any<string>(), Arg.Any<int>())
                             .Returns(x => Task.Run(() => new PagedResult<StoredMessage>
                             {
                                 CurrentPage = 1,
                                 TotalCount = 100,
                                 Result = new List<StoredMessage>
                                 {
                                     new StoredMessage(),
                                     new StoredMessage()
                                 }
                             }));
        };

        Because of = () => AsyncHelper.Run(() => MessageList.Handle(new SelectedExplorerItemChanged(new AuditEndpointExplorerItem(Endpoint)))); //trigger refresh

        It signals_work_is_started = () => EventAggregator.Received(1).Publish(Arg.Any<WorkStarted>());
        It signals_work_is_finished = () => EventAggregator.Received(1).Publish(Arg.Any<WorkFinished>());
        It loads_the_messages = () => MessageList.Messages.Count.ShouldEqual(2);
        It shows_the_search_bar = () => SearchBar.IsVisible.ShouldBeTrue();
    }

    public class loads_the_messages_from_the_queue : with_the_message_list
    {
        protected static Queue SelectedQueue;

        Establish context = () =>
        {
            SelectedQueue = new Queue("testqueue");
            MessageStore.Add(SelectedQueue, new List<MessageInfo>(new[] { new MessageInfo(), new MessageInfo() }));
        };

        Because of = () => AsyncHelper.Run(() => MessageList.Handle(new SelectedExplorerItemChanged(new QueueExplorerItem(SelectedQueue)))); // trigger refresh

        It signals_work_is_started = () => EventAggregator.Received(1).Publish(Arg.Any<WorkStarted>());
        It signals_work_is_finished = () => EventAggregator.Received(1).Publish(Arg.Any<WorkFinished>());
        It loads_the_messages = () => MessageList.Messages.Count.ShouldEqual(2);
        It hides_the_search_bar = () => SearchBar.IsVisible.ShouldBeFalse();
    }
}