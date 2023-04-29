// This file was automatiCLAYlly generated by VS extension Windows Machine Learning Code Generator v3
// from model file CLAY.onnx
// Warning: This file may get overwritten if you add add an onnx file with the same name
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.AspNetCore.Components.Forms;
using System.Xml.Linq;

namespace NeospectraMauiDemo
{

    public sealed class CLAYModel
    {

        public static async Task<CLAYModel> LoadModelAsync(string modelFileName)
        {

            CLAYModel learningModel = new CLAYModel();
            await learningModel.Load(modelFileName);
            return learningModel;
        }
        InferenceSession _session;
        async Task Load(string ModelFile)
        {
            //var assembly = GetType().Assembly;

            using var stream = await FileSystem.OpenAppPackageFileAsync(ModelFile);
            //using var reader = new BinaryReader(stream);
            byte[] allData = StreamHelper.ReadFully(stream);

            //using var modelMemoryStream = new MemoryStream(allData);
            //_model = modelMemoryStream.ToArray();
            _session = new InferenceSession(allData);


        }
        string ModelInputName = "serving_default_input_3:0";
        string ModelOutputName = "StatefulPartitionedCall:0";
        public async Task<float> ProcessOutputAsync(float[] inputData)
        {
            var inputMeta = _session.InputMetadata;
            //var input = new DenseTensor<float>(inputMeta[ModelInputName].Dimensions);
            var input = new DenseTensor<float>(inputData, new int[] { 1, 154 });
            //var ts = inputData.ToTensor<float>();
            using var results = _session.Run(new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(ModelInputName, input)
            });
            var output = results.FirstOrDefault(i => i.Name == ModelOutputName);
            var scores = output.AsTensor<float>().ToList();
            // Get the label and loss from the output
            var label = scores[0];
            // Format the output string
            string score = $"{nameof(CLAYModel)} => {label}";

            Debug.WriteLine(score); return label;

        }
    }
}

