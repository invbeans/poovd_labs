using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace poovd_lab1
{
    class ZoomImage
    {
        //maxBright - переменная для хранения максимальной яркости изображения (самый яркий участок)
        int maxBright;
        //maxBright - переменная для хранения минимальной яркости изображения (самый темный участок)
        int minBright;
        //part - переменная для хранения фрагмента изображения в масштабе 1:1
        ushort[] part;
        //width1 - изначальный размер стороны фрагмента
        int width1;
        //width2 - размер стороны фрагмента после увеличения
        int width2;
        //shift - переменная для хранения сдвига
        int shift;

        //конструктор класса, определяет минимальную и максимальную яркость передаваемого в параметрах фрагмента
        //переменные: part - фрагмент изображения, width - его ширина, zoom - во сколько раз увеличивать, shift - сдвиг
        public ZoomImage(ushort[] part, int width, int zoom, int shift)
        {
            this.part = part;
            this.width1 = width;
            //размер увеличенного фрагмента
            this.width2 = zoom * width;
            this.shift = shift;
            maxBright = 0;
            minBright = 1023;
            for(int i = 0; i < part.Length; i++)
            {
                //при нахождении новых максимальных или минимальных яркостей они присваиваются
                //соответствующим переменным
                if (part[i] < minBright) minBright = part[i];
                if (part[i] > maxBright) maxBright = part[i];
            }
        }

        //метод для масштабирования фрагмента изображения методом ближайшего соседа
        //записывает цвета пикселей в переменную класса zoomed - увеличенный фрагмент
        //возвращает одномерный массив кодов пикселей увеличенного изображения типа ushort[]
        public ushort[] ZoomNeighbour()
        {
            //инициализация переменной zoomed с размерами увеличенного фрагмента
            ushort[] zoomed = new ushort[width2 * width2];
            //соотношение изначального размера стороны и увеличенного
            double side_ratio = width1 / (double)width2;
            //переменные для хранения координат пиксела, чей цвет будет записываться в массив
            double x, y;
            int index = 0;
            for (int i = 0; i < width2; i++)
            {
                for (int j = 0; j < width2; j++)
                {
                    //получение координат по текущему счетчику и отношению сторон
                    x = Math.Floor(side_ratio * j);
                    y = Math.Floor(side_ratio * i);
                    zoomed[index] = part[(int)(y * width1 + x)];
                    index++;
                }
            }
            return zoomed;
        }

        //метод для масштабирования фрагмента изображения методом билинейной интерполяции
        //записывает цвета пикселей в переменную класса zoomed - увеличенный фрагмент
        //возвращает одномерный массив кодов пикселей увеличенного изображения типа ushort[]
        public ushort[] ZoomBilinear()
        {
            //соотношение изначального размера стороны и увеличенного
            float side_ratio = (float)(width1 - 1) / width2;
            //инициализация переменной zoomed с размерами увеличенного фрагмента
            ushort[] zoomed = new ushort[width2 * width2];
            //переменные для хранения координат пиксела, чей цвет будет записываться в массив
            int x, y;
            //переменные для хранения цветов 4 пикселов, окружающих искомый и образующих квадрат
            ushort p1, p2, p3, p4;
            //переменные для хранения расстояния между искомым пикселем и левым верхним краем квадрата
            //(между искомым и пикселем p1)
            float x_diff, y_diff;
            //счетчик для заполнения одномерного массива zoomed
            int count = 0;
            //индекс для получения четырех опорных пикселей
            int index;
            for (int i = 0; i < width2; i++)
            {
                for (int j = 0; j < width2; j++)
                {
                    //получение координат точки p1 по текущему счетчику и отношению сторон
                    x = (int)(side_ratio * j);
                    y = (int)(side_ratio * i);
                    //расстояния между искомым пикселом и верхним левым углом
                    x_diff = (j * side_ratio) - x;
                    y_diff = (i * side_ratio) - y;
                    //индекс первой точки из четырех опорных (точка p1)
                    index = y * width1 + x;
                    p1 = part[index];
                    //следующая точка в том же ряду
                    p2 = part[index + 1];
                    //точка ниже на один ряд
                    p3 = part[index + width1];
                    //точка под точкой p2
                    p4 = part[index + width1 + 1];

                    //по формуле, в которой проводится интерполяция между точками p1 и p2, точками p3 и p4, а затем между получившимися двумя цветами
                    zoomed[count] = (ushort)(p1 * (1 - x_diff) * (1 - y_diff) + p2 * (x_diff) * (1 - y_diff) + p3 * (y_diff) * (1 - x_diff) + p4 * (x_diff * y_diff));
                    count++;
                }
            }
            return zoomed;
        }

        //метод для нормирования яркостей пикселов изображения
        //возвращает полученное изображение в формате Bitmap
        //параметры: isNormalized - флаг, будут ли нормализованы яркости, zoomed - массив с увеличенным фрагментом
        private Bitmap BuildBitmap(bool isNormalized, ushort[] zoomed)
        {
            int index = 0;
            //bitmap - переменная для нового изображения после нормирования
            Bitmap bitmap = new Bitmap(width2, width2, PixelFormat.Format48bppRgb);
            for (int i = 0; i < width2; i++)
            {
                for (int j = 0; j < width2; j++)
                {
                    //получение яркости текущего пиксела (в диапазоне от 0 до 1023)
                    ushort current = zoomed[index];
                    index++;
                    ushort pixel = current;
                    //если пользователь выбрал нормирование, то оно производится
                    if (isNormalized)
                    {
                        //нормирование с диапазоном от 0 до 255
                        pixel = (ushort)((current - minBright) * 255 / (maxBright - minBright));
                    }
                    //сдвиг получившейся яркости и обнуление незначащих пикселей
                    pixel = (ushort)((pixel >> shift) & 255);                    
                    bitmap.SetPixel(j, i, Color.FromArgb(pixel, pixel, pixel));
                }
            }
            return bitmap;
        }

        //метод, к которому обращаются из других классов для построения увеличенного изображения
        //принимает флаги: isNormalized - будет ли производится нормирование, bilinear - будет ли использован
        //метод билинейной интерполяции. Возвращает изображение в формате Bitmap
        public Bitmap GetBitmap(bool isNormalized, bool bilinear)
        {
            ushort[] zoomed = new ushort[width2 * width2];
            if (bilinear) zoomed = ZoomBilinear();
            else zoomed = ZoomNeighbour();
            return BuildBitmap(isNormalized, zoomed);
        }
    }
        
}
