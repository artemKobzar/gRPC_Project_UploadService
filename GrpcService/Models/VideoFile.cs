namespace GrpcService.Models
{
    public class VideoFile
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] Data { get; set; }
    }
}
