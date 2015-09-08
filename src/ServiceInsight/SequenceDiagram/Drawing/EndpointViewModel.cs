﻿namespace ServiceInsight.SequenceDiagram.Drawing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    [DebuggerDisplay("{Name}")]
    public class EndpointViewModel : UmlViewModel, IEquatable<EndpointViewModel>
    {
        readonly DateTime order;

        protected EndpointViewModel()
        {
        }

        public EndpointViewModel(string name, string host, DateTime order, string version = null)
        {
            this.order = order;
            FullName = Title = name;
            Version = version;
            Host = host;

            Handlers = new List<HandlerViewModel>();
        }

        public string FullName { get; private set; }
        public string Version { get; private set; }
        public string Host { get; private set; }
        public List<HandlerViewModel> Handlers { get; private set; }

        public DateTime Order
        {
            get { return order; }
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode() ^ (Host ?? String.Empty).GetHashCode() ^ (Version ?? String.Empty).GetHashCode();
        }

        public bool Equals(EndpointViewModel other)
        {
            var firstPart = string.Equals(FullName, other.FullName, StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase);

            if (Version == null || other.Version == null)
            {
                return firstPart;
            }

            return firstPart && string.Equals(Version, other.Version, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            var other = obj as EndpointViewModel;

            if (other == null)
            {
                return false;
            }

            return Equals(other);
        }
    }
}
