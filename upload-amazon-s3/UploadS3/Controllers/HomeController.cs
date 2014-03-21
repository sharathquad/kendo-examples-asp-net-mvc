﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using FluentJson;
using UploadS3.Models;

namespace UploadS3.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var config = new S3Config {
                // Unique Policy ID and duration
                Uuid = Guid.NewGuid(),
                ExpirationTime = TimeSpan.FromHours(1),

                // Authentication
                AccessKey = "<YOUR_ACCESS_KEY>",
                SecretAccessKey = "<YOUR_SECRET_KEY>",

                // Bucket name and key prefix (folder)
                Bucket = "<BUCKET_NAME>",
                BucketUrl = "http://<BUCKET_NAME>.s3.amazonaws.com/",
                KeyPrefix = "<ROOT_FOLDER>/",

                // See http://docs.aws.amazon.com/AmazonS3/latest/dev/ACLOverview.html#CannedACL
                Acl = "public-read",

                // Mime type prefix
                ContentTypePrefix = "image/",

                // Fully qualified URL of an "empty document" in the same origin
                // Required for IE < 10
                SuccessUrl = "http://localhost:62629/home/success"
            };

            ViewBag.Policy = Policy(config);
            ViewBag.PolicySignature = Sign(ViewBag.Policy, config.SecretAccessKey);
            ViewBag.S3Config = config;

            return View();
        }

        public ActionResult Success()
        {
            // Return empty response to signify success to IE < 10
            return Content("", "text/plain");
        }

        private string Policy(S3Config config)
        {
            var policyJson = new JavaScriptSerializer().Serialize(new
            {
                expiration = DateTime.UtcNow.Add(config.ExpirationTime).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                conditions = new object[] {
                    new { bucket = config.Bucket },
                    new [] { "starts-with", "$key", config.KeyPrefix },
                    new { acl = config.Acl },
                    new [] { "starts-with", "$success_action_redirect", "" },
                    new [] { "starts-with", "$Content-Type", config.ContentTypePrefix },
                    new Dictionary<string, string> {{ "x-amz-meta-uuid", config.Uuid.ToString() }}
                }
            });

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(policyJson));
        }

        private static string Sign(string text, string key)
        {
            var signer = new HMACSHA1(Encoding.UTF8.GetBytes(key));
            return Convert.ToBase64String(signer.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
    }
}
