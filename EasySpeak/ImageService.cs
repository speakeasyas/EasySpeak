namespace EasySpeak
{
    public class ImageService
    {
        private readonly IWebHostEnvironment _environment;

        public ImageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveImage(byte[] image, string fileName)
        {
            // تحديد مسار المجلد wwwroot
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

            // التأكد من وجود المجلد وإنشاؤه إذا لم يكن موجودًا
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // تحديد مسار الصورة
            var imagePath = Path.Combine(uploadsFolder, fileName);

            // حفظ الصورة على الخادم
            await File.WriteAllBytesAsync(imagePath, image);

            // إرجاع عنوان URL للصورة
            return "/uploads/" + fileName;
        }
    }
}
