using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace JenkinsLib
{
    public class BuildCollection : ObservableCollection<Build>
    {
        public float AverageDuration
        {
            get
            {
                float average = 0;
                if (Count > 0 && this.Any(build => build.Result == "SUCCESS"))
                {
                    average = (float) this.Where(build => build.Result == "SUCCESS").Average(build => build.Duration);
                }

                return average;
            }
        }

        public int MaxDuration
        {
            get
            {
                int maxDuration = 0;
                if (Count > 0)
                {
                    maxDuration = this.Max(build => build.Duration);
                }
                return maxDuration;
            }
        }

        public string DisplayMaxDuration
        {
            get
            {
                var seconds = (int) MaxDuration/1000;
                if (seconds < 60)
                {
                    return $"{seconds} seconds";
                }
                return $"{seconds / 60} mins {seconds % 60} seconds";
            }
        }

        public string DisplayMinDuration
        {
            get
            {
                var seconds = (int)MinDuration / 1000;
                if (seconds < 60)
                {
                    return $"{seconds} seconds";
                }
                return $"{seconds / 60} mins {seconds % 60} seconds";
            }
        }

        public int MinDuration
        {
            get
            {
                int minDuration = 0;
                if (Count > 0)
                {
                    minDuration = this.Min(build => build.Duration);
                }
                return minDuration;
            }
        }

        public string DisplayAverageDuration
        {
            get
            {
                var seconds = (int) AverageDuration / 1000;
                // if less than 1 min
                if (seconds < 60)
                {
                    return $"{seconds} seconds";
                }
                else
                {
                    return $"{seconds / 60} mins {seconds % 60} seconds";
                }
            }
        }

        public float SuccessRate
        {
            get
            {
                var successCount = this.Count(x => x.Result == "SUCCESS");
                var rate = (float)successCount / Count;
                return rate;
            }
        }

        public int SuccessCount
        {
            get
            {
                return this.Count(x => x.Result == "SUCCESS");
            }
        }

        public int FailureCount
        {
            get
            {
                return this.Count(x => x.Result == "FAILURE"); 
            }
        }

        public int AbortCount
        {
            get
            {
                return this.Count(x => x.Result == "ABORTED");
            }
        }

        public string DisplaySuccessRate
        {
            get
            {
                var converted = Math.Round(SuccessRate*10000)/100.0;
                return $"{converted}%";
            }
        }

       

        //public float DisplayAverageDuration
        //{
        //    get
        //    {

        //    }
        //}

        public void AddRange(IEnumerable<Build> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            foreach (var i in collection) Items.Add(i);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void RemoveRange(IEnumerable<Build> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            foreach (var i in collection)
            {
                Items.Remove(i);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
