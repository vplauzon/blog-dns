using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Octokit;

namespace DnsFunction
{
    public static class DnsCheck
    {
        [FunctionName("DnsCheck")]
        public static async Task RunAsync(
            [TimerTrigger("0 */15 * * * *")]TimerInfo timer,
            //[TimerTrigger("0/5 * * * * *")]TimerInfo timer,
            ILogger log)
        {
            //  Run the first task in background while waiting for the second one
            var sourceTask = Dns.GetHostEntryAsync("vincentlauzon.com");
            var target = await Dns.GetHostEntryAsync("vplauzon.github.io");
            var source = await sourceTask;

            if (source.AddressList.ToImmutableSortedSet().SetEquals(target.AddressList))
            {
                log.LogInformation("DNS entries are identical");
            }
            else
            {
                log.LogInformation("DNS entries are different");
                log.LogInformation($"Source:  {string.Join(", ", (object[])source.AddressList)}");
                log.LogInformation($"Target:  {string.Join(", ", (object[])target.AddressList)}");

                await PushChangeAsync(target.AddressList);
            }
        }

        private static Task PushChangeAsync(IPAddress[] addressList)
        {
            var client = GetGitHubClient();

            throw new NotImplementedException();
        }

        private static GitHubClient GetGitHubClient()
        {
            var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            var basicAuth = new Credentials(githubToken);
            var client = new GitHubClient(new ProductHeaderValue("user-comment"));

            client.Credentials = basicAuth;

            return client;
        }
    }
}