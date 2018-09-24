using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace JenkinsLib
{
    public class JenkinsNodeCollection : ObservableCollection<JenkinsNode>
    {
        private JenkinsServer _jenkinsServer;

        public JenkinsServer JenkinsServer
        {
            get { return _jenkinsServer; }
            set
            {
                _jenkinsServer = value;
                //make sure to set the Username and ApiToken for each item
                foreach (var jenkinsNode in this)
                {
                    jenkinsNode.Username = _jenkinsServer.Username;
                    jenkinsNode.ApiToken = _jenkinsServer.ApiToken;
                }
            }
        }

        public void PropagateCredential(string username, string apiToken)
        {
            foreach (var jenkinsNode in this)
            {
                jenkinsNode.Username = username;
                jenkinsNode.ApiToken = apiToken;
            }
        }

        public void AddRange(IEnumerable<JenkinsNode> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            foreach (var i in collection) Items.Add(i);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void RemoveRange(IEnumerable<JenkinsNode> collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            foreach (var i in collection)
            {
                Items.Remove(i);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public int SuccessCount
        {
            get { return this.Count(j => j.Color == "blue"); }
        }
        public float SuccessRate
        {
            get
            {
                var successCount = this.Count(j => j.Color == "blue");
                var rate = (float)successCount / Count;
                return rate;
            }
        }

        public string DisplaySuccessRate
        {
            get
            {
                var converted = Math.Round(SuccessRate * 10000) / 100.0;
                return $"{converted}%";
            }
        }

        public int FailureCount
        {
            get { return this.Count(j => j.Color == "red"); }
        }

        public float FailureRate
        {
            get
            {
                var failureCount = this.Count(j => j.Color == "red");
                var rate = (float)failureCount / Count;
                return rate;
            }
        }

        public string DisplayFailureRate
        {
            get
            {
                var converted = Math.Round(FailureRate * 10000) / 100.0;
                return $"{converted}%";
            }
        }


        public int AbortCount
        {
            get { return this.Count(j => j.Color == "aborted"); }
        }

        public float AbortRate
        {
            get
            {
                var failureCount = this.Count(j => j.Color == "aborted");
                var rate = (float)failureCount / Count;
                return rate;
            }
        }

        public string DisplayAbortRate
        {
            get
            {
                var converted = Math.Round(AbortRate * 10000) / 100.0;
                return $"{converted}%";
            }
        }


        public int NoBuildCount
        {
            get { return this.Count(j => j.Color == "nobuild"); }
        }

        public int DisabledCount
        {
            get { return this.Count(j => j.Color == "disabled"); }
        }

        public float DisabledRate
        {
            get
            {
                var disabledCount = this.Count(j => j.Color == "disabled");
                var rate = (float)disabledCount / Count;
                return rate;
            }
        }

        public string DisplayDisabledRate
        {
            get
            {
                var converted = Math.Round(DisabledRate * 10000) / 100.0;
                return $"{converted}%";
            }
        }

    }
}