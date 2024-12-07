using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace FunctionAppDevoir2
{
    public class ServiceBusTriggerFunction
    {
        private readonly ILogger<ServiceBusTriggerFunction> _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public ServiceBusTriggerFunction(ILogger<ServiceBusTriggerFunction> logger, BlobServiceClient blobServiceClient)
        {
            _logger = logger;
            _blobServiceClient = blobServiceClient;
        }

        [Function(nameof(ServiceBusTriggerFunction))]
        public async Task Run(
            [ServiceBusTrigger("queue1", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            try
            {
                //Log les détails du message
                var blobFileName = message.Body.ToString();
                _logger.LogInformation("Processing message for blob: {blobFileName}", blobFileName);

                //détails stockage blob
                string sourceContainerName = "image";
                string destinationContainerName = "processed";

                //aller chercher blob client
                var sourceBlobClient = _blobServiceClient
                    .GetBlobContainerClient(sourceContainerName)
                    .GetBlobClient(blobFileName);

                var destinationBlobClient = _blobServiceClient
                    .GetBlobContainerClient(destinationContainerName)
                    .GetBlobClient(blobFileName);

                //Télécharger le blob
                var blobStream = new MemoryStream();
                await sourceBlobClient.DownloadToAsync(blobStream);
                blobStream.Position = 0;

                //Traiter le blob
                using var processedStream = ProcessBlob(blobStream);

                //upload blob traité dans la destination
                await destinationBlobClient.UploadAsync(processedStream, overwrite: true);
                _logger.LogInformation("Uploaded processed blob to: {destinationContainerName}", destinationContainerName);

                //supprimer blob original
                await sourceBlobClient.DeleteAsync();
                _logger.LogInformation("Deleted original blob from container: {sourceContainerName}", sourceContainerName);

                //Service Bus Message terminé
                await messageActions.CompleteMessageAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the blob.");
                await messageActions.DeadLetterMessageAsync(message);
            }

            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);
        }
        private Stream ProcessBlob(Stream inputStream)
        {
            //chargement de l'image depuis inputStream
            using var image = Image.Load(inputStream);

            //Redimensionner l'image
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(800, 600),
                Mode = ResizeMode.Max
            }));

            //Enregistrer nouvelle image dans nouveau stream
            var outputStream = new MemoryStream();
            image.SaveAsJpeg(outputStream);
            outputStream.Position = 0;
            return outputStream;
        }
    }
}
