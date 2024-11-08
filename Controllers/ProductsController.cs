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
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime;
using System.Dynamic;
using System.Runtime.InteropServices;
using System.Reflection;
using Tensorflow.Contexts;

//Newest Notes

namespace MyAPIProject.Controllers
{
    public interface IProcessor
    {
        Task<string> ProcessAsync();
    }

    public static class myInMemoryObject
    {
        private static readonly ExpandoObject _dynamicStorage = new ExpandoObject();
        private static readonly dynamic _dynamicObject = _dynamicStorage;
        private static RuntimeMethodHandle _jitMethodHandle;

        public static void AddProperty(string propertyName, object value)
        {
            var dictionary = (IDictionary<string, object>)_dynamicStorage;
            dictionary[propertyName] = value;
        }

        public static object GetProperty(string propertyName)
        {
            var dictionary = (IDictionary<string, object>)_dynamicStorage;
            return dictionary.TryGetValue(propertyName, out var value) ? value : null;
        }

        public static dynamic DynamicObject => _dynamicObject;

        public static void SetJitMethodHandle(RuntimeMethodHandle handle)
        {
            _jitMethodHandle = handle;
        }

        public static RuntimeMethodHandle GetJitMethodHandle()
        {
            return _jitMethodHandle;
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly PrimaryDbContext _context;

        public ProductsController(PrimaryDbContext context)
        {
            _context = context;
        }

        [HttpGet("tensorflow-hello")]
        public IActionResult TensorFlowHello()
        {
            var tensor1 = tf.constant(new float[] { 1, 2, 3 });
            var tensor2 = tf.constant(new float[] { 4, 5, 6 });
            var result = tf.add(tensor1, tensor2);
            float[] resultArray = result.numpy().ToArray<float>();
            return Ok(new
            {
                message = "Hello from TensorFlow.NET!",
                computation = "1,2,3 + 4,5,6",
                result = resultArray
            });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

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

        [HttpPost("custom-logic/{id}/{name}")]
        public async Task<IActionResult> CustomLogic([FromRoute] int id, [FromRoute] string name, [FromBody] string? inputString = null)
        {
            try
            {
                // Provide default value if inputString is null
                inputString ??= "default_input";

                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest(new { message = "Name parameter cannot be empty" });
                }

                if (id <= 0)
                {
                    return BadRequest(new { message = "Invalid ID parameter" });
                }

                var processor = ProcessorFactory.CreateProcessingChain(inputString, _context, id, name);
                var result = await processor.ProcessAsync();

                return Ok(new
                {
                    message = "Custom logic executed successfully",
                    id = id,
                    name = name,
                    inputReceived = inputString,
                    processingResult = result,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        public static class ProcessorFactory
        {
            public static IProcessor CreateProcessingChain(string input, PrimaryDbContext context, int id, string name)
            {
                Console.WriteLine($"Creating enhanced processing chain for ID: {id}, Name: {name}");
                return new ProcessingChain(input, context, id, name);
            }
        }

        private class ProcessingChain : IProcessor
        {
            private readonly string _input;
            private readonly int _id;
            private readonly string _name;
            private int _currentStage = 1;
            private readonly PrimaryDbContext _context;

            public ProcessingChain(string input, PrimaryDbContext context, int id, string name)
            {
                _input = input;
                _context = context;
                _id = id;
                _name = name;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void ForceJitCompilation(object obj)
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                RuntimeHelpers.EnsureSufficientExecutionStack();

                var handle = MethodBase.GetCurrentMethod().MethodHandle;
                RuntimeHelpers.PrepareMethod(handle);

                myInMemoryObject.SetJitMethodHandle(handle);

                RuntimeHelpers.RunClassConstructor(obj.GetType().TypeHandle);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private object RetrieveFromJitMemory()
            {
                var handle = myInMemoryObject.GetJitMethodHandle();
                RuntimeHelpers.PrepareMethod(handle);
                GC.KeepAlive(myInMemoryObject.DynamicObject);
                return myInMemoryObject.DynamicObject;
            }





            private async Task<string> ProcessStageOne()
            {
                Console.WriteLine($"Currently in Stage {_currentStage}");
                Console.WriteLine($"Stage 1: Initializing processing for input: {_input}");
                Console.WriteLine($"Processing for ID: {_id}, Name: {_name}");

                var processedInput = _input.ToUpper();
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                myInMemoryObject.AddProperty("ProcessStageOneProperty", "ProcessStageOnePropertyStringValue");
                myInMemoryObject.AddProperty("ProcessingId", _id);
                myInMemoryObject.AddProperty("ProcessingName", _name);

                ForceJitCompilation(myInMemoryObject.DynamicObject);

                var propertyValue = myInMemoryObject.GetProperty("ProcessStageOneProperty");

                try
                {
                    Console.WriteLine("Initializing TensorFlow Enviorment");
                    tf.enable_eager_execution();
                    Console.WriteLine("TensorFlow eager execution enabled");

                    Console.WriteLine("Fetching Model from database");
                    var pricingModel = await _context.TrainingModels
                        .FirstOrDefaultAsync(m => m.ModelName == "Pricing_Model");

                    if (pricingModel != null)
                    {
                        Console.WriteLine($"Pricing Model found: {pricingModel.ModelName}");
                        Console.WriteLine($"Model ID: {pricingModel.Id}");
                        Console.WriteLine($"Model Data Size: {(pricingModel.Data?.Length ?? 0)} bytes");

                        if (pricingModel.Data != null && pricingModel.Data.Length > 0)
                        {
                            Console.WriteLine("Model data is present and can be used for TensorFlow processing");

                            // Add model properties to in-memory object
                            myInMemoryObject.AddProperty("ModelId", pricingModel.Id);
                            Console.WriteLine("Successfully added Model ID to in-memory object");

                            myInMemoryObject.AddProperty("ModelName", pricingModel.ModelName);
                            Console.WriteLine("Successfully added Model Name to in-memory object");

                            myInMemoryObject.AddProperty("ModelData", pricingModel.Data);
                            Console.WriteLine("Successfully added Model Data to in-memory object");

                            // Verify properties were added
                            var storedId = myInMemoryObject.GetProperty("ModelId");
                            var storedName = myInMemoryObject.GetProperty("ModelName");
                            var storedData = myInMemoryObject.GetProperty("ModelData") as byte[];

                            Console.WriteLine($"Verification - Stored Model ID: {storedId}");
                            Console.WriteLine($"Verification - Stored Model Name: {storedName}");
                            Console.WriteLine($"Verification - Stored Data Size: {storedData?.Length ?? 0} bytes");



                        }
                        else
                        {
                            Console.WriteLine("Warning: Model found but contains no data");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No Model found in database");

                        Console.WriteLine("Fetching product data");
                        var productRecord = await _context.Products
                            .Where(p => p.IdProduct == _id && p.Name == _name)
                            .FirstOrDefaultAsync();

                        if (productRecord == null)
                        {
                            Console.WriteLine("Product not found");
                        }
                        else
                        {
                            Console.WriteLine($"Product found - ID: {productRecord.IdProduct}, Name: {productRecord.Name}");

                            Console.WriteLine("Fetching all products with the same name for training");
                            var productsByName = await _context.Products
                                .Where(p => p.Name == _name)
                                .ToListAsync();
                            ///Lets load all the prices results into a local variable 
                            var productsByNamePrices = productsByName.Select(p => (float)p.Price).ToArray();
                            /// <summary>
                            ///  From that list we will show the number of record that are aquired 
                            ///  Then we will clarify the range of of all the reconds aquired in terms
                            /// of a specified columns
                            /// </summary>
                            Console.WriteLine($"Training data initialized. Number of samples: {productsByNamePrices.Length}");
                            Console.WriteLine($"Price range: {productsByNamePrices.Min()} to {productsByNamePrices.Max()}");

                            Console.WriteLine("Initializing the creattion of Neural Network tensor becuase we have to create a model");
                            Console.WriteLine("Initializing TensorFlow tensor");
                            Tensor trainData;
                            try
                            {
                                trainData = tf.convert_to_tensor(productsByNamePrices, dtype: TF_DataType.TF_FLOAT);
                                trainData = tf.reshape(trainData, new[] { -1, 1 }); // Reshape to 2D
                                Console.WriteLine($"Tensor shape initialized: {string.Join(", ", trainData.shape)}");
                                /// <summary>
                                /// 
                                ///  Part 9 
                                ///  Lets prepare the training data
                                /// </summary>
                                Console.WriteLine("Initializing model variables");
                                var W = tf.Variable(tf.random.normal(new[] { 1, 1 }));
                                var b = tf.Variable(tf.zeros(new[] { 1 }));

                                Console.WriteLine($"Initial W shape: {string.Join(", ", W.shape)}, b shape: {string.Join(", ", b.shape)}");
                                /// <summary>
                                ///  
                                ///  Part 10 
                                ///  Then lets define the model inital specification  
                                /// </summary>
                                Console.WriteLine("Initializing training parameters");
                                int epochs = 100;
                                float learningRate = 1e-2f;
                                /// <summary>
                                /// 
                                ///  Part 11 
                                ///  After etablising the constant, training data, and model design   
                                ///  lets conduct inital training process 
                                /// </summary>
                                Console.WriteLine("Starting training process");
                                for (int epoch = 0; epoch < epochs; epoch++)
                                {
                                    try
                                    {
                                        using (var tape = tf.GradientTape())
                                        {
                                            var predictions = tf.matmul(trainData, W) + b;
                                            var loss = tf.reduce_mean(tf.square(predictions - trainData));

                                            var gradients = tape.gradient(loss, new[] { W, b });

                                            W.assign_sub(gradients[0] * learningRate);
                                            b.assign_sub(gradients[1] * learningRate);

                                            if (epoch % 10 == 0)
                                            {
                                                Console.WriteLine($"Training Epoch {epoch}, Loss: {loss.numpy()}");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error in training loop at epoch {epoch}: {ex.Message}");
                                        throw new Exception($"Training failed at epoch {epoch}", ex);
                                    }
                                }
                                /// <summary>
                                /// 
                                ///  Part 12 
                                ///  After the model is created from data from the database then trained    
                                ///  lets prepare the prediction data  
                                /// 
                                /// </summary>
                                Console.WriteLine("Training completed. Preparing for prediction.");
                                var inputArray = new float[] { (float)productRecord.Price };
                                var inputTensor = tf.convert_to_tensor(inputArray, dtype: TF_DataType.TF_FLOAT);
                                inputTensor = tf.reshape(inputTensor, new[] { -1, 1 }); // Reshape to 2D
                                /// <summary>
                                ///  
                                ///  Part 13 
                                ///  After the prediction is created we will update the PredictionDataUpdate
                                ///  object with the predicted value
                                ///   
                                /// 
                                /// </summary>
                                Console.WriteLine("Calculating prediction");
                                var prediction = tf.matmul(inputTensor, W) + b;
                                Console.WriteLine("Saving the prediction to the in-memory object");

                                // Convert prediction tensor to float value
                                float predictionValue = prediction.numpy().ToArray<float>()[0];

                                // Store the prediction and related data
                                myInMemoryObject.AddProperty("NewModel_PredictionValue", predictionValue);
                                myInMemoryObject.AddProperty("NewModel_InputPrice", productRecord.Price);
                                myInMemoryObject.AddProperty("NewModel_PredictionTimestamp", DateTime.UtcNow);

                                // Verify the stored prediction and display comprehensive results
                                var storedPrediction = myInMemoryObject.GetProperty("NewModel_PredictionValue");
                                Console.WriteLine("\n=== Prediction Results ===");
                                Console.WriteLine($"Original Price: {productRecord.Price:C}");
                                Console.WriteLine($"Predicted Value: {predictionValue:C}");
                                Console.WriteLine($"Difference: {(predictionValue - (float)productRecord.Price):C}");
                                Console.WriteLine($"Prediction Timestamp: {myInMemoryObject.GetProperty("NewModel_PredictionTimestamp")}");
                                Console.WriteLine("=======================\n");

                                // Serialize and save the model
                                Console.WriteLine("Starting model serialization process");
                                using (var memoryStream = new MemoryStream())
                                using (var writer = new BinaryWriter(memoryStream))
                                {
                                    // Write model weights
                                    var wData = W.numpy().ToArray<float>();
                                    writer.Write(wData.Length);
                                    foreach (var w in wData)
                                    {
                                        writer.Write(w);
                                    }
                                    Console.WriteLine("Model weights serialized successfully");

                                    // Write model bias
                                    var bData = b.numpy().ToArray<float>();
                                    writer.Write(bData.Length);
                                    foreach (var bias in bData)
                                    {
                                        writer.Write(bias);
                                    }
                                    Console.WriteLine("Model bias serialized successfully");

                                    // Save to in-memory object as single property
                                    myInMemoryObject.AddProperty("Pricing_Model", memoryStream.ToArray());
                                    Console.WriteLine("Model saved to in-memory object successfully");

                                    // Verify the stored model
                                    var storedModelData = myInMemoryObject.GetProperty("Pricing_Model") as byte[];
                                    Console.WriteLine($"Verification - Model Data Size: {storedModelData?.Length ?? 0} bytes");
                                }

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Tensor initialization failed: {ex.Message}");
                                throw new Exception("Failed to initialize tensor from price data.", ex);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ProcessStageOne Failed {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw;
                }

                await Task.Delay(700);

                return $"S1:[{timestamp}] Initialized: {processedInput} | Property Value: {propertyValue} | ID: {_id} | Name: {_name}";
            }












            private async Task<string> ProcessStageTwo(string input)
            {

                ///Training on Predicted  Price stored in memory for new model
                ///
                Console.WriteLine($"Currently in Stage {_currentStage}");
                Console.WriteLine($"Stage 2 (Parallel): Processing input: {input}");
                Console.WriteLine($"Processing for ID: {_id}, Name: {_name}");

                var jitObject = RetrieveFromJitMemory();
                var stageOneProperty = myInMemoryObject.GetProperty("ProcessStageOneProperty");
                Console.WriteLine($"Stage 2 Retrieved Property Value from JIT Memory: {stageOneProperty}");

                var modelData = myInMemoryObject.GetProperty("ModelData");
                var pricingModel = myInMemoryObject.GetProperty("Pricing_Model");
                object processStageTwoModelData;

                if (modelData != null)
                {
                    Console.WriteLine("MODEL FOUND IN DATABASE! Selecting ModelData STORED IN MEMORY for processing");
                    processStageTwoModelData = modelData;
                    ///TODO: Training if model is found in the database and saved to the in memeory object
                }
                else if (pricingModel != null)
                {
                    Console.WriteLine("MODEL JUST CREATED! Selecting Pricing_Model STORED IN MEMORY for processing");
                    processStageTwoModelData = pricingModel;

                    Console.WriteLine("Starting deserialization of newly created model from memory");
                    using (var memoryStream = new MemoryStream(processStageTwoModelData as byte[]))
                    using (var reader = new BinaryReader(memoryStream))
                    {
                        Console.WriteLine("Initializing binary reader for model data");

                        // Read weights
                        var weightLength = reader.ReadInt32();
                        Console.WriteLine($"Reading weights data. Length: {weightLength}");
                        var weightsData = new float[weightLength];
                        for (int i = 0; i < weightLength; i++)
                        {
                            weightsData[i] = reader.ReadSingle();
                        }
                        Console.WriteLine("Weights data successfully read from binary stream");

                        // Read bias
                        var biasLength = reader.ReadInt32();
                        Console.WriteLine($"Reading bias data. Length: {biasLength}");
                        var biasData = new float[biasLength];
                        for (int i = 0; i < biasLength; i++)
                        {
                            biasData[i] = reader.ReadSingle();
                        }
                        Console.WriteLine("Bias data successfully read from binary stream");

                        // Convert to TensorFlow variables
                        Console.WriteLine("Converting weights to TensorFlow tensor");
                        var W = tf.Variable(weightsData, shape: new[] { 1, 1 }, name: "weights");
                        Console.WriteLine($"Weights tensor created with shape: {string.Join(", ", W.shape)}");

                        Console.WriteLine("Converting bias to TensorFlow tensor");
                        var b = tf.Variable(biasData, shape: new[] { 1 }, name: "bias");
                        Console.WriteLine($"Bias tensor created with shape: {string.Join(", ", b.shape)}");

                        Console.WriteLine("Model parameters successfully loaded into TensorFlow variables");
                        Console.WriteLine($"Verification - Weight variable shape: {string.Join(", ", W.shape)}, Bias variable shape: {string.Join(", ", b.shape)}");
                    }
                    ///Todo: Prepare for reinforcement learing with Clustering

                    }
                else
                {
                    Console.WriteLine("In Memory instance model data is not found");
                    processStageTwoModelData = null;
                }

                myInMemoryObject.AddProperty("Added_String_ProcessStageTwo", "myString");
                ForceJitCompilation(myInMemoryObject.DynamicObject);
                Console.WriteLine($"Stage 2 Added New Property to JIT Memory: Added_String_ProcessStageTwo");

                var words = input.Split(' ');
                var processed = words.Select(w => w.Length > 3 ? char.ToUpper(w[0]) + w.Substring(1) : w);
                var result = string.Join(" ", processed);

                await Task.Delay(1000);

                return $"S2:[Capitalization] {result} | JIT Retrieved Property: {stageOneProperty} | ID: {_id} | Name: {_name}";
            }














            private async Task<string> ProcessStageThree(string input)
            {

                ///Training on Clustered Price Derrived from Clustering 
                ///
                Console.WriteLine($"Currently in Stage {_currentStage}");
                Console.WriteLine($"Stage 3 (Parallel): Processing input: {input}");
                Console.WriteLine($"Processing for ID: {_id}, Name: {_name}");

               



                await Task.Delay(1500);

                return $"ID: {_id} | Name: {_name}";
            }



            private async Task<string> ProcessStageFour(string input)
            {
                ///Training on Clustered Price Derrived from Clustering 
                ///
                Console.WriteLine($"Currently in Stage {_currentStage}");
                Console.WriteLine($"Stage 4 (Parallel): Processing input: {input}");
                Console.WriteLine($"Processing for ID: {_id}, Name: {_name}");

               

                await Task.Delay(800);

                return $"ID: {_id} | Name: {_name}";
            }







            private async Task<string> ProcessStageFive(IEnumerable<string> results)
            {
                Console.WriteLine($"Currently in Stage {_currentStage}");
                Console.WriteLine($"Stage 5: Final processing of results");
                Console.WriteLine($"Processing for ID: {_id}, Name: {_name}");

                var sb = new StringBuilder();
                sb.AppendLine("=== Processing Summary ===");
                sb.AppendLine($"Processing ID: {_id}");
                sb.AppendLine($"Processing Name: {_name}");

                foreach (var result in results)
                {
                    sb.AppendLine($"- {result}");
                }

                sb.AppendLine($"Total Processing Time: {DateTime.UtcNow.ToString("HH:mm:ss.fff")}");
                sb.AppendLine("=== End Summary ===");

                await Task.Delay(500);

                return sb.ToString();
            }



            public async Task<string> ProcessAsync()
            {
                _currentStage = 1;
                var firstStageResult = await ProcessStageOne();

                _currentStage = 2;
                var parallelTasks = new[]
                {
                    ProcessStageTwo(_input),
                    ProcessStageThree(_input),
                    ProcessStageFour(_input)
                };

                var parallelResults = await Task.WhenAll(parallelTasks);

                _currentStage = 5;
                var allResults = new List<string> { firstStageResult };
                allResults.AddRange(parallelResults);
                return await ProcessStageFive(allResults);
            }
        }





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










        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.IdProduct }, product);
        }





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