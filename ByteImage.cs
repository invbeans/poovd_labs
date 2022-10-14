using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace poovd_lab1
{
    class ByteImage
    {
        //массив байтов, прочитанных из файла
        private byte[] originalBytes;
        //массив ushort кодов пикселей с 10 значащами битами
        public ushort[] originalPixels;
        //получаемые из файла ширина и высота изображения
        public int Width { get; private set; }
        public int Height { get; private set; }

        //конструктор класса, принимающий строку с полным путем файла
        public ByteImage(string path)
        {
            //заполнение массива байтами из файла
            originalBytes = File.ReadAllBytes(path);
            //чтение двух байтов по правилу "от младшего к старшему"
            Width = (int)((originalBytes[0]) | (originalBytes[1] << 8));
            Height = (int)((originalBytes[2]) | (originalBytes[3] << 8));
            //получение массива яркостей пикселей изображения
            BuildImageArray();
        }

        //метод для построения изображений с заданными сдвигом и верхним рядом
        //принимает сдвиг - shift и верхний ряд изображения - highestY
        //возвращает изображение в формате Bitmap
        public Bitmap GetBitmap(int shift, int highestY)
        {
            //инициализация переменной bitmap нужного размера
            Bitmap bitmap = new Bitmap(Width, Height - highestY, PixelFormat.Format48bppRgb);
            //переменная для хранения текущего индекса в массиве originalPixels
            int index = highestY * Width;
            for (int y = highestY; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    //применение сдвига и обнуление незначащих бит величины яркости
                    ushort current = (ushort)((originalPixels[index] >> shift) & 255);
                    bitmap.SetPixel(x, y - highestY, Color.FromArgb(current, current, current));
                    index++;
                }
            }
            return bitmap;
        }

        //метод для построения массива, объединяющего числа из соседних пар байтов,
        //который последовательно хранит цвета всех пикселов изображения
        private void BuildImageArray()
        {
            //создание массива для хранения всего изображения
            originalPixels = new ushort[Width * Height];
            //переменная i проходит по соседним парам байтов
            for (int i = 4, j = 0; i < Width * Height * 2 + 4; i += 2, j++)
            {
                //два байта объединяются по правилу "от младшего к старшему"
                //незначащие старшие 6 бит обнуляются
                originalPixels[j] = (ushort)((originalBytes[i] | (originalBytes[i + 1] << 8)) & 1023);
            }
        }

        //метод для получения яркости пиксела под курсором мыши
        //принимает текущую точку point
        //возвращает яркость пиксела, величина которой находится в диапазоне от 0 до 1023
        public ushort GetColorByPixel(Point point)
        {
            //получение яркости из одномерного массива с изначальными яркостями
            return originalPixels[point.Y * Width + point.X];
        }

        //метод для построения обзорного изображения
        //принимает в параметрах shift - сдвиг кодов изображения
        //возвращает обзорное изображение в формате Bitmap
        public Bitmap BuildOverviewImage(int shift)
        {
            //m - переменная, определяющая прорезание строк и рядов
            const int m = 5;
            //переменная bitmap - обзорное изображение
            Bitmap bitmap = new Bitmap((int)(Width / m), (int)(Height / m), PixelFormat.Format48bppRgb);
            int y = 0;
            int x;
            //проход по строкам и рядам с шагом m
            for (int i = 0; i < Height; i += m)
            {
                x = 0;
                for (int j = 0; j < Width; j += m)
                {
                    //получение яркости пиксела исходного изображения с сдвигом кодов и обнулением старших незначащих битов
                    ushort current = ((ushort)((originalPixels[i * Width + j] >> shift) & 255));
                    bitmap.SetPixel(x, y, Color.FromArgb(current, current, current));
                    x++;
                }
                y++;
            }
            return bitmap;
        }
    }
}
