using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using ITI.Grpc.Protos;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using static ITI.Grpc.Protos.InventoryServiceProto;

namespace ITI.Grpc.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class StoreController : ControllerBase
	{
		private readonly IConfiguration _config;
		private readonly string _apiKey;
		private readonly InventoryServiceProtoClient _client;

		public StoreController(IConfiguration config)
		{
			_config = config;
			_apiKey = _config["ApiKey"];

			var channel = GrpcChannel.ForAddress("https://localhost:7214", new GrpcChannelOptions
			{
				Credentials = ChannelCredentials.Create(new SslCredentials(), CallCredentials.FromInterceptor(AddApiKey))
			});

			_client = new InventoryServiceProtoClient(channel);
		}

		private Task AddApiKey(AuthInterceptorContext context, Metadata metadata)
		{
			metadata.Add("x-api-key", _apiKey);
			return Task.CompletedTask;
		}

		[HttpPost]
		public async Task<ActionResult> AddProduct(Product product)
		{
			try
			{
				var isExisted = await _client.GetProductByIdAsync(new Id { Id_ = product.Id });

				if (!isExisted.IsExisted_)
				{
					var addedProduct = await _client.AddProductAsync(product);
					return Created("Product Created", addedProduct);
				}

				var updatedProduct = await _client.UpdateProductAsync(product);
				return Created("Product Updated", updatedProduct);
			}
			catch (RpcException rpcEx)
			{
				Console.WriteLine($"gRPC error: {rpcEx.Status.Detail}");
				return StatusCode((int)rpcEx.StatusCode, $"gRPC error: {rpcEx.Status.Detail}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Server error: {ex.Message}");
				return StatusCode(500, $"Server error: {ex.Message}");
			}
		}

		[HttpPost("addproducts")]
		public async Task<ActionResult> AddBulkProducts(List<Product> products)
		{
			try
			{
				var call = _client.AddBulkProducts();

				foreach (var product in products)
				{
					await call.RequestStream.WriteAsync(product);
					await Task.Delay(100); // Simulate some delay for streaming
				}

				await call.RequestStream.CompleteAsync();
				var response = await call.ResponseAsync;

				return Ok(response);
			}
			catch (RpcException rpcEx)
			{
				Console.WriteLine($"gRPC error: {rpcEx.Status.Detail}");
				return StatusCode((int)rpcEx.StatusCode, $"gRPC error: {rpcEx.Status.Detail}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Server error: {ex.Message}");
				return StatusCode(500, $"Server error: {ex.Message}");
			}
		}

		[HttpGet("GetReport")]
		public async Task<ActionResult> GetReport()
		{
			try
			{
				var call = _client.GetProductReport(new Empty());
				var products = new List<Product>();

				while (await call.ResponseStream.MoveNext(CancellationToken.None))
				{
					products.Add(call.ResponseStream.Current);
				}

				return Ok(products);
			}
			catch (RpcException rpcEx)
			{
				Console.WriteLine($"gRPC error: {rpcEx.Status.Detail}");
				return StatusCode((int)rpcEx.StatusCode, $"gRPC error: {rpcEx.Status.Detail}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Server error: {ex.Message}");
				return StatusCode(500, $"Server error: {ex.Message}");
			}
		}
	}
}
