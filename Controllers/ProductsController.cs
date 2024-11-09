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
using Accord.MachineLearning;
using Accord.Math;
using Accord.Math.Distances;


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

                ///Sample In-Memory Implementation
                myInMemoryObject.AddProperty("ProcessStageOneProperty", "ProcessStageOnePropertyStringValue");
                myInMemoryObject.AddProperty("ProcessingId", _id);
                myInMemoryObject.AddProperty("ProcessingName", _name);
                ForceJitCompilation(myInMemoryObject.DynamicObject);
                var propertyValue = myInMemoryObject.GetProperty("ProcessStageOneProperty");

                try
                {
                    ///Prep TF Enviorment
                    Console.WriteLine("Initializing TensorFlow Enviorment");
                    tf.enable_eager_execution();
                    Console.WriteLine("TensorFlow eager execution enabled");

                    ///Initial Detection of Model in Database
                    Console.WriteLine("Fetching Model from database");
                    var pricingModel = await _context.TrainingModels
                        .FirstOrDefaultAsync(m => m.ModelName == "Pricing_Model");

                    if (pricingModel != null)
                    {
                        ///Identification of the model
                        Console.WriteLine($"Pricing Model found: {pricingModel.ModelName}");
                        Console.WriteLine($"Model ID: {pricingModel.Id}");
                        Console.WriteLine($"Model Data Size: {(pricingModel.Data?.Length ?? 0)} bytes");

                        ///Detect if the Model also Has Data from the Database
                        if (pricingModel.Data != null && pricingModel.Data.Length > 0)
                        {
                            /// <summary>
                            /// MODEL FOUND Part A
                            /// </summary>
                            Console.WriteLine("Model data is present and can be used for TensorFlow processing");
                            /// <summary>
                            /// RETRIEVED Neural Network Training from Data Retrieved from Database 
                            /// In Progress......
                            /// </summary>
                            // Add model properties to in-memory object
                            myInMemoryObject.AddProperty("ModelId", pricingModel.Id);
                            Console.WriteLine("Successfully added Model ID to in-memory object");

                            myInMemoryObject.AddProperty("ModelName", pricingModel.ModelName);
                            Console.WriteLine("Successfully added Model Name to in-memory object");

                            myInMemoryObject.AddProperty("Data", pricingModel.Data);
                            Console.WriteLine("Successfully added Data to in-memory object");

                            // Verify properties were added
                            var storedId = myInMemoryObject.GetProperty("ModelId");
                            var storedName = myInMemoryObject.GetProperty("ModelName");
                            var storedData = myInMemoryObject.GetProperty("Data") as byte[];

                            Console.WriteLine($"Verification - Stored Model ID: {storedId}");
                            Console.WriteLine($"Verification - Stored Model Name: {storedName}");
                            Console.WriteLine($"Verification - Stored Data Size: {storedData?.Length ?? 0} bytes");

                            /// <summary>
                            /// RETRIEVED Data Clustering creation and training from Data Retrieved from Database 
                            /// In Progress......
                            /// </summary>
                        }
                        else
                        {
                            Console.WriteLine("Warning: Model found but contains no data");
                        }
                    }
                    else
                    {
                        /// <summary>
                        /// MODEL NOT FOUND Part B
                        /// </summary>
                        Console.WriteLine("No Model found in database");

                        /// <summary>
                        /// PREPARE THE DATA 
                        /// </summary>

                        /// NEW Get THE PRODUCT from database to train on - 1
                        /// Selection Data Set with constraint
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
                            /// NEW Get THE PRODUCT from database to train on - 2
                            Console.WriteLine($"Product found - ID: {productRecord.IdProduct}, Name: {productRecord.Name}");

                            /// NEW Get PRODUCTS from database to train on - 1
                            /// Categorical DATA Set with constraints
                            Console.WriteLine("Fetching all products with the same name for training");
                            var productsByName = await _context.Products
                                .Where(p => p.Name == _name)
                                .ToListAsync();

                            /// <summary>
                            /// Structure Input Params for training model 
                            /// </summary>
                            /// NEW Get PRODUCTS from database to train on Lets load all the prices results into a local variable -2

                            /// Names
                            var productByNameNames = productsByName.Select(p => p.Name).ToArray();
                            /// Prices
                            var productsByNamePrices = productsByName.Select(p => (float)p.Price).ToArray();

                            /// <summary>
                            /// NEW Get PRODUCTS from database to train on - 3
                            /// From that list we will show the number of record that are acquired 
                            /// Then we will clarify the range of all the records acquired in terms
                            /// of specified columns
                            /// </summary>
                            Console.WriteLine($"Training data initialized. Number of samples: {productsByNamePrices.Length}");
                            Console.WriteLine($"Price range: {productsByNamePrices.Min()} to {productsByNamePrices.Max()}");

                            /// <summary>
                            /// MODEL ARCHITECTURE MODIFICATIONS FOR COMBINED FEATURES
                            /// 
                            /// 1. Input Layer Changes:
                            ///    - Original: Single input dimension (price only)
                            ///    - Modified: Multiple input dimensions (price + one-hot encoded names)
                            ///    - Input shape: [batch_size, 1 + number_of_unique_names]
                            /// 
                            /// 2. Weight Matrix (W) Modifications:
                            ///    - Original: Shape was [1, 1] for price only
                            ///    - Modified: Shape is [input_dim, 1] where input_dim = 1 + uniqueNames.Count
                            ///    - Each row in W now corresponds to:
                            ///      * Row 0: Price weight
                            ///      * Row 1 to N: Weights for each unique name
                            /// 
                            /// 3. Bias Vector (b) Adjustments:
                            ///    - Maintains shape [1] but now accounts for combined features
                            ///    - Acts on the weighted sum of all features
                            /// 
                            /// 4. Forward Pass Calculation:
                            ///    - Original: prediction = price_data * W + b
                            ///    - Modified: prediction = concat(price_data, name_data) * W + b
                            ///    - Matrix multiplication now incorporates both price and name influences
                            /// 
                            /// 5. Training Considerations:
                            ///    - Loss function remains MSE but operates on higher dimensional inputs
                            ///    - Gradient updates affect both price and name feature weights
                            ///    - Learning rate applied uniformly across all feature weights
                            /// </summary>

                            /// <summary>
                            /// NEW Neural Network creation and training from Data Retrieved from Database 
                            /// </summary>
                            Console.WriteLine("Phase One: Initializing the creation of Neural Network...");
                            Console.WriteLine("Initializing tensors for both prices and names");
                            Tensor priceTrainData;
                            Tensor nameTrainData;
                            try
                            {
                                /// <summary>
                                /// Create separate tensors for prices and names
                                /// Names are encoded as indices and then one-hot encoded
                                /// </summary>
                                priceTrainData = tf.convert_to_tensor(productsByNamePrices, dtype: TF_DataType.TF_FLOAT);
                                priceTrainData = tf.reshape(priceTrainData, new[] { -1, 1 }); // Reshape to 2D

                                // Create dictionary for name encoding
                                var uniqueNames = productByNameNames.Distinct().ToList();
                                var nameToIndex = uniqueNames.Select((name, index) => new { name, index })
                                                           .ToDictionary(x => x.name, x => x.index);

                                // Convert names to indices
                                var nameIndices = productByNameNames.Select(name => nameToIndex[name]).ToArray();

                                /// <summary>
                                /// One-hot encoding process:
                                /// 1. Creates a matrix of size [num_samples, num_unique_names]
                                /// 2. Each row represents one sample
                                /// 3. Each column represents one unique name
                                /// 4. Matrix contains 1.0 where name matches, 0.0 elsewhere
                                /// </summary>
                                var oneHotNames = new float[nameIndices.Length, uniqueNames.Count];
                                for (int i = 0; i < nameIndices.Length; i++)
                                {
                                    oneHotNames[i, nameIndices[i]] = 1.0f;
                                }

                                nameTrainData = tf.convert_to_tensor(oneHotNames, dtype: TF_DataType.TF_FLOAT);

                                /// <summary>
                                /// Feature combination process:
                                /// 1. Concatenate price and name tensors along axis 1 (columns)
                                /// 2. Results in tensor of shape [num_samples, 1 + num_unique_names]
                                /// 3. Each row contains: [price, name_1_hot, name_2_hot, ..., name_n_hot]
                                /// </summary>
                                var combinedTrainData = tf.concat(new[] { priceTrainData, nameTrainData }, axis: 1);

                                Console.WriteLine($"Combined tensor shape: {string.Join(", ", combinedTrainData.shape)}");

                                /// <summary>
                                /// Modified model initialization:
                                /// 1. inputDim = 1 (price) + uniqueNames.Count (one-hot encoded names)
                                /// 2. W matrix shape: [inputDim, 1] for mapping combined features to price
                                /// 3. b vector shape: [1] for single output bias
                                /// </summary>
                                Console.WriteLine("Initializing model variables for combined features");
                                var inputDim = 1 + uniqueNames.Count; // Price dimension + name dimensions
                                var W = tf.Variable(tf.random.normal(new[] { inputDim, 1 }));
                                var b = tf.Variable(tf.zeros(new[] { 1 }));

                                Console.WriteLine($"Modified W shape: {string.Join(", ", W.shape)}, b shape: {string.Join(", ", b.shape)}");

                                /// <summary>
                                /// Part 2 
                                /// Then lets define the model initial specification  
                                /// </summary>
                                Console.WriteLine("Initializing training parameters");
                                int epochs = 100;
                                float learningRate = 1e-2f;

                                /// <summary>
                                /// Part 3
                                /// After establishing the constant, training data, and model design   
                                /// lets conduct initial training process 
                                /// </summary>
                                Console.WriteLine("Starting training process with combined features");
                                for (int epoch = 0; epoch < epochs; epoch++)
                                {
                                    try
                                    {
                                        using (var tape = tf.GradientTape())
                                        {
                                            var predictions = tf.matmul(combinedTrainData, W) + b;
                                            // Modified loss function to work with combined features
                                            var loss = tf.reduce_mean(tf.square(predictions - priceTrainData));

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
                                /// Part 4 
                                /// After the model is created from data from the database then trained    
                                /// lets prepare the prediction data with combined features
                                /// </summary>
                                Console.WriteLine("Training completed. Preparing for prediction with combined features.");
                                var inputPrice = new float[] { (float)productRecord.Price };
                                var inputName = new float[uniqueNames.Count];
                                inputName[nameToIndex[productRecord.Name]] = 1.0f;

                                var combinedInput = tf.concat(new[] {
                                tf.convert_to_tensor(new[] { inputPrice }, dtype: TF_DataType.TF_FLOAT),
                                tf.convert_to_tensor(new[] { inputName }, dtype: TF_DataType.TF_FLOAT)
                                                         }, axis: 1);

                                /// <summary>
                                /// Part 5 
                                /// After the prediction is created we will update the IN MEMORY OBJECT
                                /// object with the predicted value
                                /// </summary>
                                Console.WriteLine("Calculating prediction with combined features");
                                var prediction = tf.matmul(combinedInput, W) + b;
                                float predictionValue = prediction.numpy().ToArray<float>()[0];

                                // Store the prediction and related data
                                myInMemoryObject.AddProperty("NewModel_PredictionValue", predictionValue);
                                myInMemoryObject.AddProperty("NewModel_InputPrice", productRecord.Price);
                                myInMemoryObject.AddProperty("NewModel_PredictionTimestamp", DateTime.UtcNow);
                                myInMemoryObject.AddProperty("NewModel_NameFeatures", uniqueNames.Count);
                                myInMemoryObject.AddProperty("NewModel_TotalFeatures", inputDim);

                                /// <summary>
                                /// Part 6 
                                /// Verify Saving of Prediction values    
                                /// </summary>
                                var storedPrediction = myInMemoryObject.GetProperty("NewModel_PredictionValue");
                                Console.WriteLine("\n=== Prediction Results ===");
                                Console.WriteLine($"Original Price: {productRecord.Price:C}");
                                Console.WriteLine($"Predicted Value: {predictionValue:C}");
                                Console.WriteLine($"Difference: {(predictionValue - (float)productRecord.Price):C}");
                                Console.WriteLine($"Prediction Timestamp: {myInMemoryObject.GetProperty("NewModel_PredictionTimestamp")}");
                                Console.WriteLine($"Number of Name Features: {myInMemoryObject.GetProperty("NewModel_NameFeatures")}");
                                Console.WriteLine($"Total Features: {myInMemoryObject.GetProperty("NewModel_TotalFeatures")}");
                                Console.WriteLine("=======================\n");

                                /// <summary>
                                /// Part 7 
                                /// Save the Model to the in-Runtime memory     
                                /// </summary>
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

                                    // Save to in-memory object as separate properties
                                    myInMemoryObject.AddProperty("ModelName", "Pricing_Model");
                                    myInMemoryObject.AddProperty("Data", memoryStream.ToArray());
                                    Console.WriteLine("Model name and data saved to in-memory object successfully");

                                    // Verify the stored model
                                    var storedModelName = myInMemoryObject.GetProperty("ModelName");
                                    var storedModelData = myInMemoryObject.GetProperty("Data") as byte[];
                                    Console.WriteLine($"Verification - Model Name: {storedModelName}");
                                    Console.WriteLine($"Verification - Model Data Size: {storedModelData?.Length ?? 0} bytes");

                                    /// <summary>
                                    /// NEW Data Clustering creation and training from Data Retrieved from Database 
                                    /// </summary>
                                    Console.WriteLine("Phase two: Initializing Data K Clustering Implementation");

                                    Console.WriteLine($"Found {productsByName.Count} products with name '{_name}' for Data Clustering");

                                    /// Extract prices and convert to double
                                    Console.WriteLine("Extracting prices for clustering");
                                    var prices = productsByName.Select(p => new double[] { (double)p.Price }).ToArray();

                                    /// Define clustering parameters
                                    int numClusters = 3; // Ensure we always have 3 clusters
                                    int numIterations = 100;
                                    Console.WriteLine($"Clustering parameters: clusters={numClusters}, iterations={numIterations}");

                                    /// Create k-means algorithm
                                    var kmeans = new Accord.MachineLearning.KMeans(numClusters)
                                    {
                                        MaxIterations = numIterations,
                                        Distance = new SquareEuclidean()
                                    };
                                    /// Compute the algorithm
                                    Console.WriteLine("Starting k-means clustering");
                                    var clusters = kmeans.Learn(prices);

                                    /// Get the cluster centroids
                                    var centroids = clusters.Centroids;

                                    Console.WriteLine("K-means clustering completed");

                                    /// Get cluster assignments for each point
                                    var assignments = clusters.Decide(prices);

                                    /// Log final results
                                    Console.WriteLine("Final clustering results:");
                                    for (int i = 0; i < prices.Length; i++)
                                    {
                                        Console.WriteLine($"Price: {prices[i][0]:F4}, Cluster: {assignments[i]}");
                                    }

                                    Console.WriteLine("Final centroids:");
                                    for (int i = 0; i < numClusters; i++)
                                    {
                                        Console.WriteLine($"Centroid {i}: {centroids[i][0]:F4}");
                                    }
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