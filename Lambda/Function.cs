using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Web;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using MindTouch.LambdaSharp;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Helpers;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]


namespace Lambda
{
    class Function: ALambdaFunction<Message, string>
    {
        //--Fields--
        private Random _rand;
        private S3Bucket _audioBucket;
        private S3Bucket _textBucket;
        private AmazonPollyClient _pollyClient;
        private AmazonTranscribeServiceClient _transcribeClient;

        //--Methods--
        public override Task InitializeAsync(LambdaConfig config) {
            _rand = new Random();
            _textBucket = new S3Bucket(config.ReadText("TextForPolly"));
            _audioBucket = new S3Bucket(config.ReadText("AudioForTranscribe"));
            _pollyClient = new AmazonPollyClient();
            _transcribeClient = new AmazonTranscribeServiceClient();
            return Task.CompletedTask;
        }
        public override async Task<String> ProcessMessageAsync(Message message, ILambdaContext context)
        {
    

            // Create describe voices request.

            // Optional use only english voices.

            // Ask Polly to describe available voices.


            // Create speech synthesis request.

            // Select Random voice for synthesis.

            // Ask Polly to generate speech.

            // Ensure audio file is in an s3 bucket for Transcribe to consume.

            // Create Transcription Job Request

            // Initiate Transcription Job

            // Wait for transcription job to complete

            // Retrieve text file from s3 and extract the text

            return message.Text;

        }
    }
}