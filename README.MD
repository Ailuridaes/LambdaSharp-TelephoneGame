# The λ# Telephone Game

Did you ever play the game Telephone as a child, it's where a group of children sit in a circle and take turns whispering a phrase to the person to their right. With even a small group of say 5 children what the first child says and what the last child hear are two completely different phrases.

Tonight we are going to use 2 extremely powerful tools from AWS to simulate this children's game: Polly, the AWS Text-to-Speech service as well as Transcribe the Speech-To-Text service. As always Lambda is the central component, and we are also using S3 in a supporting role. 

This Repo contains 2 directories `Helpers` and `Lambda` as well as a `Module.yml` file and this readme. The `Helpers` directory contains 2 helper classes Message and S3Bucket to help keep the focus on the core 2 services and not on setup. The message class is to make deserializing the event JSON straightforward and S3Bucket which has a `BucketName` property for easy access as well as 2 methods `PutObjectAsync(string key, string filePath)` and `GetObjectAsync(string key)`.

The Lambda directory contains our Lambda Function with all the boilerplate required for the λ# tool to function. It also initializes all clients.

## Helpful Links
[.NET AWS SDK Docs](https://docs.aws.amazon.com/sdkfornet/v3/apidocs)

[Polly API Actions](https://docs.aws.amazon.com/polly/latest/dg/API_Operations.html)

[Transcribe API Actions](https://docs.aws.amazon.com/transcribe/latest/dg/API_Operations.html)


# Pre-requisites
The following tools and accounts are required to complete these instructions.

* [Sign-up for an AWS account](https://aws.amazon.com/)
* [Install AWS CLI](https://aws.amazon.com/cli/)
* [Install .NET Core 2.1](https://www.microsoft.com/net/download)

# Level 0 Set up λ# tool
First fork/clone the Challenge Repo then run `dotnet restore` from the `LambdaSharp-TelephoneGame/Lambda` folder

The λ# CLI is installed as a [.NET Global Tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools).

```bash
dotnet tool install -g MindTouch.LambdaSharp.Tool --version 0.4.0.2
```

NOTE: with the following 3 commands use the --aws-profile arguement if you wish/need to use an AWS profile other than `default`

Once installed, the λ# CLI needs to be configured.
```bash
dotnet lash config
```

Next, a deployment tier must be initialized with the λ# runtime.
```bash
dotnet lash init --tier Sandbox
```

Finally deploy the base code provided via the λ#
```bash
dotnet lash deploy --tier Sandbox
```
You should get output that looks like
```bash
MindTouch LambdaSharp CLI (v0.4.0.2) - Deploy LambdaSharp module
Readying module for deployment tier 'tylera'

Compiling module: Module.yml
=> Building function Lambda [netcoreapp2.1, Release]
=> Module compilation done
Publishing module: TelephoneGame
=> Uploading function: s3://lambdasharptool-tylera-deploymentbucket-1ck6cr7gfo5e4/Modules/TelephoneGame/Assets/function_Lambda_B7131D5DE072900BC26205E6219607C5.zip
=> Uploading template: s3://lambdasharptool-tylera-deploymentbucket-1ck6cr7gfo5e4/Modules/TelephoneGame/Assets/cloudformation_v1.0_AE67B75C485EB4D13CEA14735664CD3A.json
=> Uploading manifest: s3://lambdasharptool-tylera-deploymentbucket-1ck6cr7gfo5e4/Modules/TelephoneGame/Assets/manifest_v1.0_AE67B75C485EB4D13CEA14735664CD3A.json
Deploying stack: tylera-TelephoneGame [TelephoneGame]
=> Stack creation initiated for tylera-TelephoneGame
CREATE_IN_PROGRESS                  AWS::CloudFormation::Stack                              tylera-TelephoneGame (User Initiated)
CREATE_IN_PROGRESS                  AWS::S3::Bucket                                         AudioForTranscribe
CREATE_IN_PROGRESS                  Custom::LambdaSharpRegisterModule                       ModuleRegistration
CREATE_IN_PROGRESS                  AWS::S3::Bucket                                         TextForPolly
CREATE_IN_PROGRESS                  AWS::S3::Bucket                                         AudioForTranscribe (Resource creation Initiated)
CREATE_IN_PROGRESS                  AWS::IAM::Role                                          CloudWatchLogsRole
CREATE_IN_PROGRESS                  AWS::S3::Bucket                                         TextForPolly (Resource creation Initiated)
CREATE_IN_PROGRESS                  AWS::IAM::Role                                          CloudWatchLogsRole (Resource creation Initiated)
CREATE_COMPLETE                     AWS::IAM::Role                                          CloudWatchLogsRole
CREATE_COMPLETE                     AWS::S3::Bucket                                         AudioForTranscribe
CREATE_COMPLETE                     AWS::S3::Bucket                                         TextForPolly
CREATE_IN_PROGRESS                  Custom::LambdaSharpRegisterModule                       ModuleRegistration (Resource creation Initiated)
CREATE_COMPLETE                     Custom::LambdaSharpRegisterModule                       ModuleRegistration
CREATE_IN_PROGRESS                  AWS::IAM::Role                                          ModuleRole
CREATE_IN_PROGRESS                  AWS::IAM::Role                                          ModuleRole (Resource creation Initiated)
CREATE_COMPLETE                     AWS::IAM::Role                                          ModuleRole
CREATE_IN_PROGRESS                  AWS::Lambda::Function                                   Lambda
CREATE_IN_PROGRESS                  AWS::Lambda::Function                                   Lambda (Resource creation Initiated)
CREATE_COMPLETE                     AWS::Lambda::Function                                   Lambda
CREATE_IN_PROGRESS                  AWS::Logs::LogGroup                                     LambdaLogGroup
CREATE_IN_PROGRESS                  Custom::LambdaSharpRegisterFunction                     LambdaRegistration
CREATE_IN_PROGRESS                  AWS::Logs::LogGroup                                     LambdaLogGroup (Resource creation Initiated)
CREATE_COMPLETE                     AWS::Logs::LogGroup                                     LambdaLogGroup
CREATE_IN_PROGRESS                  Custom::LambdaSharpRegisterFunction                     LambdaRegistration (Resource creation Initiated)
CREATE_COMPLETE                     Custom::LambdaSharpRegisterFunction                     LambdaRegistration
CREATE_IN_PROGRESS                  AWS::Logs::SubscriptionFilter                           LambdaLogGroupSubscription
CREATE_IN_PROGRESS                  AWS::Logs::SubscriptionFilter                           LambdaLogGroupSubscription (Resource creation Initiated)
CREATE_COMPLETE                     AWS::Logs::SubscriptionFilter                           LambdaLogGroupSubscription
CREATE_COMPLETE                     AWS::CloudFormation::Stack                              tylera-TelephoneGame
=> Stack creation finished (finished: 2018-12-04 14:14:01)
Stack output values:
=> ModuleName: TelephoneGame
=> ModuleVersion: 1.0

Done (duration: 00:01:20.5880550)
```

To test this is working open up the Lambda console find your function named `{tier}-TelephoneGame-Lambda-{random-cloudformation-string}` And execute a test event with JSON that looks like:
```
{
    "Identifier": "StringUsedToIdentifyFiles",
    "Text" : "StringToBeTransformedToSpeechAndBack"
}
```
The lambda function should return the string you fed in as "Text"

The remaining steps all involve editing the `ProcessMessageAsync` function located in `Lambda/Function.cs`

# Level 1 Polly
- Use Polly to turn text to speech

    - Ask Polly to describe available voices.
        - Create describe voices request.
        - Optional use only english voices.
        - Run request to describe available voices.


    - Ask Polly to synthesize speech.
        - Create speech generation request
        - Select Random voice for synthesis.
        - Ask Polly to generate speech.

    - Ensure the returned audio file ends up in the `AudioForTranscribe` bucket
        - Bucket name is easily accessible via `_audioBucket.BucketName`       
        - the `PutObjectAsync` method in S3Bucket.cs may be useful depending on you implementation.
(Polly docs)
(C# SDK)

Redeploy via `dotnet lash deploy --tier {tier}` and at this point you should be able to run the lambda and get an audio file in s3 containing the spoken version of the "Text" field.

# Level 2 Transcribe pt.1 The Request
- Use Transcribe to turn the audio file back to text
    - Create a Transcription job request
    - Provide it the s3 uri of the file we/Polly put in s3
        - Bucket name is accessible via `_audioBucket.BucketName`
    - Run the Request

Redeploy via `dotnet lash deploy --tier {tier}` and at this point you should be able to run the lambda and get a JSON file in the TextForPolly bucket that contains the transcribed string under json["results"]["transcripts"][0]["transcript"]

### Note:
    Transcription Job Names need to be unique per account and will be used as the key for the s3 object in the `_textBucket`

# Level 3 Transcribe pt.2 Waiting and Parsing JSON
- Return transcribed string from Transcription file
    - Wait for Transcibe job to finish
        - Wait 5 seconds
        - Hit Transcribe for jobStatus
        - If jobStatus is "COMPLETED" move on
    - Fetch file from S3
        - Bucket name is accessible via `_textBucket.BucketName`
        - Key is  `{TranscriptionJobName}`.json
        - Finally the S3Bucket class has a `GetObjectAsync` method see `Helpers/S3Bucket.cs`
    - Parse the JSON
    - Return the string

Redeploy via `dotnet lash deploy --tier {tier}` and at this point you should be able to run the lambda and return the transformed string

# Level 4 Feedback / BOSS Level Feedback ad Infinitum 
At this point all the data transformation work is done. But the telephone game only worsks because the information is passed multiple times and gathers translation artifacts. So we need to be able to loop through a specified number of times by providing an `Iterations` value in the message JSON. 

Before continuing you should pick if you want to do the Level 4 ending or the BOSS ending.

This challenge was originally conceived and developed when AWS max timeout was 300 seconds, with an average cycle of Text-Speech-Text taking 90-120 seconds meaning solving for an acceptable number of Transformations say 5 required some gymnastics. In October the max timeout was raised to 15 minutes which allows 7-10 iterations and enough for the game to work its magic. 

The boss level is to build a solution that is capable of looping to any arbitrary value of `Iterations` without timing out while keeping the 5 min time out. Level 4 is to build to it loop a few times but will timeout eventually with a large enough value of `Iterations` even when increasing the timeout period to maximum

## Level 4
- Loop through a specified number of tranformations of Text-Speech-Text
    - Increase the timeout in `Module.yml` to 900
    - Loop through the process the specified number of times
    - Make sure to append a versioning number to the end of the Identifier so all s3 keys and transcription job identifiers are unique

## BOSS LEVEL
- Be able to do any number of Text-Speech-Text transformations
    - Figure out a way around the 300 second timeout
    - I would test on probably 5 iterations as that should exceed 300 seconds
    - This will likely require initializing new clients which means adding permissions for the new service in `Module.yml` as well as initializing the new client in the `InitialyzeAsync` method.

# TEARDOWN
Because this was all deployed via cloudformation stacks teardown is easy, first delete everything in the 2 S3 buckets. Then navigate to the cloudformation page in the AWS console, select the stack named `{tier}-TelephoneGame`, click `Actions` and then `Delete Stack`. Then delete the stack `{tier}-LambdaSharp` and optionally remove the cli profile stack named `LambdaSharpTool-{cli-profile}`
