namespace Api.Services
{
    public class JsonRpcRequest
    {
        public string Jsonrpc { get; set; } = "2.0";
        public object Id { get; set; }
        public string Method { get; set; }
        public object Params { get; set; }
    }

    public class JsonRpcResponse
    {
        public string Jsonrpc { get; set; } = "2.0";
        public object Id { get; set; }
        public object Result { get; set; }
        public JsonRpcError Error { get; set; }
    }

    public class JsonRpcError
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}

