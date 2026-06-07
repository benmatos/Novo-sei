using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using NovoSei.Core.Interfaces;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NovoSei.Infrastructure.Services;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3FileStorageService(IConfiguration configuration)
    {
        var accessKey = configuration["Storage:S3:AccessKey"] ?? string.Empty;
        var secretKey = configuration["Storage:S3:SecretKey"] ?? string.Empty;
        var serviceUrl = configuration["Storage:S3:ServiceUrl"] ?? string.Empty;
        _bucketName = configuration["Storage:S3:BucketName"] ?? "novosei-bucket";

        var config = new AmazonS3Config();
        if (!string.IsNullOrEmpty(serviceUrl))
        {
            config.ServiceURL = serviceUrl;
            config.ForcePathStyle = true;
        }

        if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
        {
            _s3Client = new AmazonS3Client(accessKey, secretKey, config);
        }
        else
        {
            _s3Client = new AmazonS3Client(config);
        }
    }

    public async Task<string> SalvarArquivoAsync(string nomeArquivo, byte[] conteudo)
    {
        NovoSei.Core.Services.FileSecurityValidator.ValidarArquivo(nomeArquivo, conteudo);
        try
        {
            var listBuckets = await _s3Client.ListBucketsAsync();
            if (!listBuckets.Buckets.Any(b => b.BucketName == _bucketName))
            {
                await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = _bucketName });
            }
        }
        catch
        {
            // Ignorado em ambientes de produção corporativos onde o bucket é pré-provisionado
        }

        using var ms = new MemoryStream(conteudo);
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = nomeArquivo,
            InputStream = ms
        };

        await _s3Client.PutObjectAsync(request);
        return nomeArquivo;
    }

    public async Task<byte[]?> ObterArquivoAsync(string caminho)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = caminho
            };

            using var response = await _s3Client.GetObjectAsync(request);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeletarArquivoAsync(string caminho)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = caminho
            };
            await _s3Client.DeleteObjectAsync(request);
        }
        catch
        {
            // Opcional: ignorar ou logar erro ao deletar
        }
    }
}
