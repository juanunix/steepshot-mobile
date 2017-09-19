﻿using System.Collections.Generic;
using Ditch.Operations.Post;

namespace Steepshot.Core.Models.Responses
{
    ///{
    ///  "title": "cat",
    ///  "tags": [
    ///    "cat1",
    ///    "cat2",
    ///    "cat3",
    ///    "cat4",
    ///    "steepshot"
    ///  ],
    ///  "body": "http://res.cloudinary.com/steepshot2/image/upload/v1484584571/bxfru7z88aeqkvermoiw.jpg"
    ///}
    public class ImageUploadResponse
    {
        public string Title { get; set; }

        public List<string> Tags { get; set; }

        public string Body { get; set; }

        public string Permlink { get; set; }
    }

    //    {
    //    "payload": {
    //        "title": "test",
    //        "tags": [
    //            "steepshot",
    //            "test",
    //            "steepshot"
    //        ],
    //        "username": "joseph.kalu",
    //        "body": "http://qa.steepshot.org/api/v1/image/02273668-788e-46cc-949b-cabde9bc2830.jpeg"
    //    },
    //    "meta": {
    //        "tags": [
    //            "steepshot",
    //            "test",
    //            "steepshot"
    //        ],
    //        "app": "steepshot/0.0.5",
    //        "extensions": [
    //            [
    //                0,
    //                {
    //                    "beneficiaries": [
    //                        {
    //                            "account": "steepshot",
    //                            "weight": 1000
    //                        }
    //                    ]
    //                }
    //            ]
    //        ]
    //    }
    //}

    public class UploadResponse
    {
        public ImageUploadResponse Payload { get; set; }

        public object Meta { get; set; }

        public Beneficiary[] Beneficiaries { get; set; }
    }
}