using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using JenkinsLib;
using JenkinsToolsetWpf.Properties;

namespace JenkinsToolsetWpf
{
    internal class JobConfigFileWatcher
    {
        private JenkinsNodeCollection _jenkinsNodeCollection;
        private Timer _timer;

        public JobConfigFileWatcher()
        {
            _timer = new Timer(Tick, null, 1000, 1000);
        }

        public JenkinsNodeCollection JenkinsNodesToWatch
        {
            get
            {
                if (_jenkinsNodeCollection == null)
                    _jenkinsNodeCollection = new JenkinsNodeCollection();
                return _jenkinsNodeCollection;
            }
            set { _jenkinsNodeCollection = value; }
        }

        private void Tick(object state)
        {
            var editedJobs = JenkinsNodesToWatch.Where(job => job.LocalConfigFilePath != null &&
                                                      !string.IsNullOrEmpty(job.OriginalConfigXml) &&
                                                      job.State != JobState.New &&
                                                      File.Exists(job.LocalConfigFilePath));

            lock (editedJobs)
            {
                foreach (var job in editedJobs)
                {
                    try
                    {
                        string modifiedConfigXml;

                        if (!IsFileLocked(new FileInfo(job.LocalConfigFilePath)))
                        {
                            modifiedConfigXml = File.ReadAllText(job.LocalConfigFilePath);
                        }
                        else
                        {
                            continue;
                        }

                        // If it is marked updated, but changed back to original, then mark it back to original
                        if (!string.IsNullOrEmpty(modifiedConfigXml) && (job.State == JobState.UpdatedLocally) &&
                            job.OriginalConfigXml == modifiedConfigXml)
                        {
                            job.State = JobState.Orginal;
                            continue;
                        }

                        // If it changed, mark it updated locally.
                        if (!string.IsNullOrEmpty(modifiedConfigXml) && (job.OriginalConfigXml != modifiedConfigXml))
                        {
                            job.State = JobState.UpdatedLocally;
                        }
                    }
                    catch (Exception exp)
                    {
                        Debug.WriteLine(exp.ToString());
                    }
                }
            }

        }

        private bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }

            //file is not locked
            return false;
        }

        public void CleanTempFiles()
        {
            var editedJobs =
                JenkinsNodesToWatch.Where(
                    job => !string.IsNullOrEmpty(job.LocalConfigFilePath) && File.Exists(job.LocalConfigFilePath));
            foreach (var job in editedJobs)
            {
                try
                {
                    if (Settings.Default.LocalTempDirectory == Path.GetDirectoryName(job.LocalConfigFilePath))
                    {
                        File.Delete(job.LocalConfigFilePath);
                    }
                }
                catch (Exception exp)
                {
                    Debug.WriteLine(exp.ToString());
                }
            }
        }
    }
}