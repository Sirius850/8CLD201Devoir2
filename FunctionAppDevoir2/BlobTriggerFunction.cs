using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using System.Text;

namespace FunctionAppDevoir2
{
    public class BlobTriggerFunction
    {
        private readonly ILogger<BlobTriggerFunction> _logger;

        public BlobTriggerFunction(ILogger<BlobTriggerFunction> logger)
        {
            _logger = logger;
        }

        [Function(nameof(BlobTriggerFunction))]
        public async Task Run([BlobTrigger("image/{name}", Connection = "AzureWebJobsStorage")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}");

            //Envoi du nom du fichier dans la queue Azure Bus
            var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
            var client = new ServiceBusClient(serviceBusConnectionString);
            var sender = client.CreateSender("queue1");

            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(name));
            await sender.SendMessageAsync(message);

            _logger.LogInformation($"Sent message to the Service Bus queue: {name}");
        }
    }
}
