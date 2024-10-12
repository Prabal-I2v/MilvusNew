using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Milvus.Client;
using MilvusTestServer;

[ApiController]
[Route("api/[controller]")]
public class milvusController : ControllerBase
{
    private readonly MilvusService _milvusService;

    public milvusController(MilvusService milvusService)
    {
        _milvusService = milvusService;
    }
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            return Ok();
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    [HttpGet]
    [Route("create-collection")]
    public async Task<IActionResult> CreateCollection([FromQuery] string collectionName, [FromQuery] int dimension)
    {
        try
        {
            var res = await _milvusService.CreateCollectionAsync(collectionName, dimension);
            return Ok(res);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost]
    [Route("insert-data")]
    public async Task<IActionResult> InsertData([FromQuery] string collectionName, [FromBody] List<InsertDataRequest> data)
    {
        try
        {
            var res = await _milvusService.InsertDataAsync(collectionName, data);
            return Ok(res);

        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost]
    [Route("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest searchRequestParams)
    {
        try
        {
            var res = await _milvusService.SearchAsync(searchRequestParams);
            return Ok(res);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost]
    [Route("create-index")]
    public async Task<IActionResult> CreateIndex([FromBody] CreateIndexRequest request)
    {
        try
        {
            var res = await _milvusService.CreateIndexAsync(
                request
            );
            return Ok(res);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet]
    [Route("remove-index")]
    public async Task<IActionResult> RemoveIndex([FromQuery] string collectionName, [FromQuery] string columnName)
    {
        try
        {
            var res = await _milvusService.RemoveIndexAsync(collectionName, columnName);
            return Ok(res);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }

    
    [HttpGet]
    [Route("load-collection")]
    public async Task<IActionResult> LoadCollection([FromQuery] string collectionName)
    {
        try
        {
            var res = await _milvusService.LoadCollectionAsync(collectionName);
            return Ok(res);

        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet]
    [Route("total-data")]
    public async Task<IActionResult> GetTotalData([FromQuery] string collectionName)
    {
        try
        {
            var res = await _milvusService.GetTotalData(collectionName);
            return Ok(res);

        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet]
    [Route("release-collection")]
    public async Task<IActionResult> ReleaseCollection([FromQuery] string collectionName)
    {
        try
        {
            var res = await _milvusService.ReleaseCollectionAsync(collectionName);
            return Ok(res);

        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet]
    [Route("describe-collection")]
    public async Task<IActionResult> DescribeCollectionAsync([FromQuery] string collectionName)
    {
        try
        {
            var res = await _milvusService.DescribeCollectionAsync(collectionName);
            return Ok(res);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet]
    [Route("deleteDataByProperties")]
    public async Task<IActionResult> DeleteDataByProperties([FromQuery] string collectionName, [FromQuery] string expression)
    {
        try
        {
            var res = await _milvusService.DeleteDataByProperties(collectionName, expression);
            return Ok(res);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet]
    [Route("deleteDataByPrimaryId")]
    public async Task<IActionResult> DeleteDataByPrimaryId([FromQuery] string collectionName, [FromQuery] string expression)
    {
        try
        {
            var res = await _milvusService.DeleteDataByPrimaryId(collectionName, expression);
            return Ok(res);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet]
    [Route("deleteAllData")]
    public async Task<IActionResult> DeleteAllData([FromQuery] string collectionName, [FromQuery] string expression)
    {
        try
        {
            var res = await _milvusService.DeleteAllData(collectionName, expression);
            return Ok(res);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet]
    [Route("getAllCollections")]
    public async Task<IActionResult> GetCollectionsAsync()
    {
        try
        {
            var res = await _milvusService.GetCollectionsAsync();
            return Ok(res);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet]
    [Route("connect")]
    public async Task<IActionResult> ConnectToMilvus(string ip, int port)
    {
        try
        {
            var result = await _milvusService.ConnectToMilvusAsync(ip, port);
            if (result)
            {
                return Ok(new { success = true, message = "Connection successful" });
            }
            else
            {
                return BadRequest(new {success = false, message = "Connection failed" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(ex.Message);
        }
    }
}