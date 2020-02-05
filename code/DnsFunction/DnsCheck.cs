using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Octokit;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

namespace DnsFunction
{
    public static class DnsCheck
    {
        [FunctionName("DnsCheck")]
        public static async Task RunAsync(
            [TimerTrigger("0 */15 * * * *")]TimerInfo timer,
            //[TimerTrigger("0/1 * * * * *")]TimerInfo timer,
            ILogger log)
        {
            //  Run the first task in background while waiting for the second one
            var sourceTask = Dns.GetHostEntryAsync("vincentlauzon.com");
            var target = await Dns.GetHostEntryAsync("vplauzon.github.io");
            var source = await sourceTask;

            if (Compare(target.AddressList, source.AddressList))
            {
                log.LogInformation("DNS entries are identical");
            }
            else
            {
                log.LogInformation("DNS entries are different");
                log.LogInformation($"Source:  {string.Join(", ", (object[])source.AddressList)}");
                log.LogInformation($"Target:  {string.Join(", ", (object[])target.AddressList)}");

                await PushChangeAsync(target.AddressList, log);
            }
        }

        private static bool Compare(IPAddress[] source, IPAddress[] target)
        {
            var sourceText = from ip in source
                             select ip.ToString();
            var targetText = from ip in target
                             select ip.ToString();

            return sourceText.ToImmutableSortedSet().SetEquals(targetText);
        }

        private static async Task PushChangeAsync(IPAddress[] addressList, ILogger log)
        {
            const string REPO_OWNER = "vplauzon";
            const string REPO_NAME = "shared-infra-dns";

            var client = GetGitHubClient();
            var repo = await client.Repository.Get(REPO_OWNER, REPO_NAME);
            var parametersContent = await client.Repository.Content.GetAllContentsByRef(
                repo.Id,
                "dns.parameters.json",
                "master");

            if (parametersContent.Count != 1)
            {
                throw new InvalidOperationException(
                    $"DNS Parameters content count is {parametersContent.Count}");
            }

            var content = parametersContent[0].Content;

            log.LogInformation($"Old content:  {content}");
            var fileMap = JsonSerializer.Deserialize<IDictionary<string, object>>(content);
            var parameters = (JsonElement)fileMap["parameters"];
            var nonBlogIps = from p in parameters.EnumerateObject()
                             where p.Name != "blogIps"
                             select KeyValuePair.Create(p.Name, (object)p.Value);
            var newBlogIps = from ip in addressList
                             select new { ipv4Address = ip.ToString() };
            var newBlogIpsPair = KeyValuePair
                .Create("blogIps", (object)new { value = newBlogIps });
            var newParameters = new Dictionary<string, object>(
                nonBlogIps.Append(newBlogIpsPair));

            fileMap["parameters"] = newParameters;

            var newContent = JsonSerializer.Serialize(
                fileMap,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            log.LogInformation($"New content:  {newContent}");

            // Create file request
            var fileRequest = new UpdateFileRequest(
                $"IPs changed by Azure Function",
                newContent,
                parametersContent[0].Sha,
                "master")
            {
                Committer = new Committer("ip-reset", "ip-reset@ip-reset.com", DateTime.Now)
            };

            await client.Repository.Content.UpdateFile(repo.Id, "dns.parameters.json", fileRequest);
        }

        private static GitHubClient GetGitHubClient()
        {
            var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            var basicAuth = new Credentials(githubToken);
            var client = new GitHubClient(new ProductHeaderValue("reset-ip"));

            client.Credentials = basicAuth;

            return client;
        }
    }
}