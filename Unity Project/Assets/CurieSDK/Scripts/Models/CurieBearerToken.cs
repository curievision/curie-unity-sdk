using System;

namespace CurieSDK
{
    [Serializable]
    public class CurieBearerToken
    {
        public string access_token;
        public string token_type;
        public int expires;
        public string user_id;
        public string org_id;
    }

}
