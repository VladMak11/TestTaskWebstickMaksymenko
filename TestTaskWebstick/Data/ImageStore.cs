using TestTaskWebstick.Models;


namespace TestTaskWebstick.Data
{
    public class ImageStore
    {
        public static List<Image> imageList = new List<Image> {
         new Image{ Id = 1, Url="Url1"},
          new Image{ Id = 2, Url="Url2"},
           new Image{ Id = 3, Url="Url3"}
        };
    }
}
