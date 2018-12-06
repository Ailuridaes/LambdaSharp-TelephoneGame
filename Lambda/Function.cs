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
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]


namespace Lambda {
    public class Function : ALambdaEventFunction<Message> {

        //--Fields--
        private string _audioBucket;
        private string _textBucket;
        private string _topic;
        private Random _rand;
        private AmazonS3Client _s3Client;
        private AmazonPollyClient _pollyClient;
        private AmazonTranscribeServiceClient _transcribeClient;
        private AmazonSimpleNotificationServiceClient _snsClient;

        //--Methods--
        public override Task InitializeAsync(LambdaConfig config) {
            _audioBucket = AwsConverters.ConvertBucketArnToName(config.ReadText("AudioForTranscribe"));
            _textBucket = AwsConverters.ConvertBucketArnToName(config.ReadText("TextForPolly"));
            _topic = config.ReadText("Loop");
            _rand = new Random();
            _s3Client =  new AmazonS3Client();
            _pollyClient = new AmazonPollyClient();
            _transcribeClient = new AmazonTranscribeServiceClient();
            _snsClient = new AmazonSimpleNotificationServiceClient();
            return Task.CompletedTask;
        }

        public override async Task ProcessMessageAsync(Message message, ILambdaContext context) {
            if(message.Iterations < 0) {
                return;
            }
            var suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            // Initiate describe voices request.
            var describeVoiceResponse = await _pollyClient.DescribeVoicesAsync(new DescribeVoicesRequest {
                // LanguageCode = "en-US"
            });
            // LogInfo(JsonConvert.SerializeObject(describeVoiceResponse.Voices));
            var randomIndex = _rand.Next(describeVoiceResponse.Voices.Count);
            var randomVoice = describeVoiceResponse.Voices[randomIndex];
            LogInfo($"Selected random voice '{randomVoice.Name}' in {randomVoice.LanguageName}");

            // Initiate speech synthesis request.
            var synthesizeResponse = await _pollyClient.SynthesizeSpeechAsync(new SynthesizeSpeechRequest {
                VoiceId = randomVoice.Id,
                OutputFormat = OutputFormat.Mp3,
                Text = message.Text
            });

            var audioStream = new MemoryStream();
            await synthesizeResponse.AudioStream.CopyToAsync(audioStream);
            audioStream.Position = 0;

            // Ensure audio file is in an s3 bucket for Transcribe to consume.
            await _s3Client.PutObjectAsync(new PutObjectRequest {
                BucketName = _audioBucket,
                Key = $"polly_{suffix}.mp3",
                InputStream = audioStream
            });

            // Initiate Transcription Job
            var transcriptionName = $"transcribe_{suffix}";
            var transcriptionResponse = await _transcribeClient.StartTranscriptionJobAsync(new StartTranscriptionJobRequest {
                LanguageCode = "en-US",
                MediaFormat = MediaFormat.Mp3,
                Media = new Media {
                    MediaFileUri = $"https://s3-us-east-1.amazonaws.com/{_audioBucket}/polly_{suffix}.mp3"
                },
                OutputBucketName = _textBucket,
                TranscriptionJobName = transcriptionName
            });

            // Wait for transcription job to complete
            while(true) {
                await Task.Delay(TimeSpan.FromSeconds(3));
                var transcriptionJobStatusReponse = await _transcribeClient.GetTranscriptionJobAsync(new GetTranscriptionJobRequest {
                    TranscriptionJobName = transcriptionName
                });
                if(transcriptionJobStatusReponse.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.FAILED) {
                    LogWarn("Transcribe job failed");
                    return;
                }
                if(transcriptionJobStatusReponse.TranscriptionJob.TranscriptionJobStatus == TranscriptionJobStatus.COMPLETED) {
                    LogInfo("Job Completed!!");
                    break;
                }
                LogInfo("Checking job status... again.");
            }

            // Retrieve text file from s3 and extract the text
            var s3Response = await _s3Client.GetObjectAsync(new GetObjectRequest {
                BucketName = _textBucket,
                Key = $"{transcriptionName}.json"
            });
            var text = await new StreamReader(s3Response.ResponseStream).ReadToEndAsync();
            LogInfo(text);
            var json = JObject.Parse(text);
            var transcription = json["results"]["transcripts"][0]["transcript"].ToString();
            LogInfo(transcription);

            if(message.Iterations >= 0) {
                await _snsClient.PublishAsync(new PublishRequest {
                    TopicArn = _topic,
                    Message = JsonConvert.SerializeObject(new Message {
                        Iterations = message.Iterations - 1,
                        Text = transcription
                    })
                });
            }

            // :)
        }
    }
}