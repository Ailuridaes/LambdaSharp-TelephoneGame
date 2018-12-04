/*
 * MindTouch λ#
 * Copyright (C) 2018 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace Helpers {

    public class S3Bucket {

        //--- Constants ---
        //--- Properties ---
        public readonly string BucketName;
        private readonly IAmazonS3 _s3Client;


        //--- Constructors ---
        public S3Bucket(string bucketArn, IAmazonS3 s3Client = null) {
            BucketName = bucketArn.Split(':').Last() ?? throw new ArgumentNullException(nameof(bucketArn));
            _s3Client = s3Client ?? new AmazonS3Client();
        }

        //--- Methods ---
        public async Task<PutObjectResponse> PutObjectAsync(String key, String filePath) {

            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = key,
                FilePath = filePath
            };
            
            // Put object
            return await _s3Client.PutObjectAsync(request);
        }

        public async Task<GetObjectResponse> GetObjectAsync(String key) {

            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = key,
            };
            
            // Put object
            return await _s3Client.GetObjectAsync(request);
        }
    }
}