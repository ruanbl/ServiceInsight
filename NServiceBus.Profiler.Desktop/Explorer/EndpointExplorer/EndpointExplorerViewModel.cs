﻿using System.Linq;
using System.Threading.Tasks;
using Caliburn.PresentationFramework;
using Caliburn.PresentationFramework.ApplicationModel;
using Caliburn.PresentationFramework.Screens;
using Caliburn.PresentationFramework.Views;
using NServiceBus.Profiler.Common.Settings;
using NServiceBus.Profiler.Core.Management;
using NServiceBus.Profiler.Core.Settings;
using NServiceBus.Profiler.Desktop.Events;

namespace NServiceBus.Profiler.Desktop.Explorer.EndpointExplorer
{
    [View(typeof(IExplorerView))]
    public class EndpointExplorerViewModel : Screen, IEndpointExplorerViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IManagementService _managementService;
        private readonly IManagementConnectionProvider _managementConnection;
        private bool _isFirstActivation = true;
        private IExplorerView _view;

        public EndpointExplorerViewModel(
            IEventAggregator eventAggregator, 
            ISettingsProvider settingsProvider,
            IManagementConnectionProvider managementConnection,
            IManagementService managementService)
        {
            _eventAggregator = eventAggregator;
            _settingsProvider = settingsProvider;
            _managementService = managementService;
            _managementConnection = managementConnection;
            Items = new BindableCollection<ExplorerItem>();
        }

        public virtual IObservableCollection<ExplorerItem> Items { get; private set; }

        public virtual ExplorerItem ServiceRoot
        {
            get { return Items.FirstOrDefault(x => x is ServiceExplorerItem); }
        }

        public virtual ExplorerItem AuditRoot
        {
            get { return ServiceRoot != null ? ServiceRoot.Children.OfType<AuditEndpointExplorerItem>().First() : null; }
        }

        public virtual ExplorerItem ErrorRoot
        {
            get { return ServiceRoot != null ? ServiceRoot.Children.OfType<ErrorEndpointExplorerItem>().First() : null; }
        }

        public int SelectedRowHandle { get; set; }

        public virtual ExplorerItem SelectedNode { get; set; }

        public virtual string ServiceUrl { get; private set; }

        private bool IsConnected
        {
            get { return ServiceUrl != null; }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            if (_isFirstActivation)
            {
                _view.ExpandNode(ServiceRoot);
                _isFirstActivation = false;
            }
        }

        public async override void AttachView(object view, object context)
        {
            base.AttachView(view, context);
            _view = view as IExplorerView;

            var configuredAddress = GetConfiguredAddress();
            var existingUrl = _managementConnection.Url;

            if (!IsConnected)
            {
                var available = await ServiceAvailable(configuredAddress);
                if (available)
                {
                    ConnectToService(configuredAddress);
                }
                else
                {
                    ConnectToService(existingUrl);
                }
            }
        }

        private string GetConfiguredAddress()
        {
            var appSettings = _settingsProvider.GetSettings<ProfilerSettings>();
            if (appSettings != null && appSettings.LastUsedManagementApi != null)
                return appSettings.LastUsedManagementApi;

            var managementConfig = _settingsProvider.GetSettings<Management>();
            return string.Format("http://{0}:{1}/api", managementConfig.Hostname, managementConfig.Port);
        }

        private async Task<bool> ServiceAvailable(string serviceUrl)
        {
            _managementConnection.ConnectTo(serviceUrl);

            var connected = await _managementService.IsAlive();

            return connected;
        }

        private async void AddServiceNode()
        {
            if (ServiceRoot == null)
            {
                Items.Add(new ServiceExplorerItem(ServiceUrl));
            }

            ServiceRoot.Children.Clear();

            var endpoints = await _managementService.GetEndpoints();
            
            if(endpoints == null)
                return;

            foreach (var endpoint in endpoints)
            {
                ServiceRoot.Children.Add(new AuditEndpointExplorerItem(endpoint));
            }

            _view.ExpandNode(ServiceRoot);
        }

        public virtual void OnSelectedNodeChanged()
        {
            _eventAggregator.Publish(new SelectedExplorerItemChanged(SelectedNode));
        }

        public void ConnectToService(string url)
        {
            if(url == null)
                return;

            ServiceUrl = url;
            AddServiceNode();
        }

        public async Task FullRefresh()
        {
            
        }

        public async Task PartialRefresh()
        {
        }

        public void Handle(AutoRefreshBeat message)
        {
            PartialRefresh();
        }

    }
}