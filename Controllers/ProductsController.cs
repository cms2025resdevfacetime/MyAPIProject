using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAPIProject.Models;
using Tensorflow;
using Tensorflow.NumPy;
using static Tensorflow.Binding;

namespace MyAPIProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly PrimaryDbContext _context;

        public ProductsController(PrimaryDbContext context)
        {
            _context = context;
        }

        // GET: api/Products/tensorflow-hello
        [HttpGet("tensorflow-hello")]
        public IActionResult TensorFlowHello()
        {
            // Create a simple TensorFlow computation
            var tensor1 = tf.constant(new float[] { 1, 2, 3 });
            var tensor2 = tf.constant(new float[] { 4, 5, 6 });
            var result = tf.add(tensor1, tensor2);
            // Convert NDArray to regular array before serialization
            float[] resultArray = result.numpy().ToArray<float>();
            return Ok(new
            {
                message = "Hello from TensorFlow.NET!",
                computation = "1,2,3 + 4,5,6",
                result = resultArray
            });
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return product;
        }

        // POST: api/Products/custom-logic
        [HttpPost("custom-logic")]
        public async Task<IActionResult> CustomLogic([FromBody] string inputString)
        {
            try
            {
                if (string.IsNullOrEmpty(inputString))
                {
                    return BadRequest(new { message = "Input string cannot be empty" });
                }

                // TODO: Add your custom logic here
                // Example of how you might use the inputString:
                // - Process the string
                // - Use it for database queries
                // - Pass it to TensorFlow operations

                return Ok(new
                {
                    message = "Custom logic executed successfully",
                    inputReceived = inputString,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.IdProduct)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.IdProduct }, product);
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.IdProduct == id);
        }
    }
}