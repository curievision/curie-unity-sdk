using System;
using System.Collections.Generic;

namespace CurieSDK
{
    [Serializable]
    public class Product
    {
        public string id;
        public string name;
        public string brand;
        public string description;
        public object tags;
        public string thumbnail_url;
        public object uids;
        public string variants;
        public string default_variant_id;
        public string created_on;
        public string updated_on;
        public List<ProductFile> files;
    }

}
