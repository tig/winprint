namespace libvt100
{
    public interface IAnsiDecoder : IDecoder
    {
        void Subscribe ( IAnsiDecoderClient _client );
        void UnSubscribe ( IAnsiDecoderClient _client );
    }
}
