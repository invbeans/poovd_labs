using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace poovd_lab1
{
    public partial class Form1 : Form
    {
        //isZoomChosen - флаг, определяющий, выбран ли фрагмент для увеличения - изначально нет
        bool isZoomChosen = false;

        //part - массив яркостей масштабируемого фрагмента в масштабе 1:1
        ushort[] part;

        //ширина и высота увеличиваемого фрагмента
        int width = 60;
        int height = 60;

        //image - класс для хранения массива байтов и построения изображений, в
        //конструкторе принимает строку с путем к файлу
        private ByteImage image;

        //img - переменная для хранения построенных с помощью методов
        //класса ByteImage изображений
        private Bitmap img;

        //zoomImage - объект класса, предоставляющего методы для работы с изображениями формата Bitmap
        private ZoomImage zoomImage;

        //highestRow - переменная для хранения верхнего ряда изображения
        //изначально равна 0 - изображение строится полностью
        private int highestRow = 0;


        //конструктор формы с инициализацией приложения и подключение
        //описанных ниже обработчиков событий к элементам управления
        public Form1()
        {
            InitializeComponent();
            loadButton.Click += LoadButton_Click;
            scrollStepInput.TextChanged += ScrollStepInput_TextChanged;
            radioShift1.CheckedChanged += RadioShift1_CheckedChanged;
            radioShift2.CheckedChanged += RadioShift2_CheckedChanged;
            radioShift3.CheckedChanged += RadioShift3_CheckedChanged;
            imageContainer.MouseMove += ImageContainer_MouseMove;
            imageContainer.MouseClick += ImageContainer_Click;
        }

        private void Form1_Load(object sender, EventArgs e) {}

        //метод для выбора изображения в окне Проводника,
        //возвращает строку - путь к выбранному изображению
        private string OpenExplorer()
        {
            OpenFileDialog fileDialog = new OpenFileDialog()
            {
                //разрешается выбирать только файлы .mbv формата
                Filter = "Mbv Files (*.mbv) | *.mbv",
                RestoreDirectory = true
            };
            //если пользователь ничего не выбрал - возвращается
            //пустая строка
            if (fileDialog.ShowDialog() != DialogResult.OK)
                return "";
            //верхний ряд обнуляется
            highestRow = 0;
            //название файла приравнивается пустой строке
            highestRowInput.Text = "";            
            return fileDialog.FileName;
        }

        //метод для получения выбранного с помощью радиокнопок сдвига
        //кодов изображения. Возвращает число - количество бит сдвига
        private int GetCurrentShift()
        {
            //по умолчанию и в случае, когда ни одна из кнопок не
            //выбрана, сдвиг равен нулю
            int shift = 0;
            //список всех радиокнопок группы codeShiftBox
            List<RadioButton> shiftButtons = codeShiftBox.Controls.OfType<RadioButton>().ToList();
            foreach (var button in shiftButtons)
            {
                if (button.Checked)
                {
                    //если кнопка выбрана - получение числа из ее свойства Текст
                    shift = int.Parse(button.Text);
                }
            }
            return shift;
        }

        //метод получения текущего выбранного масштаба увеличения
        //возвращает число - во сколько раз будет увеличено изображение
        private int GetZoomValue()
        {
            //по умолчанию зум равен 3
            int zoom = 3;
            List<RadioButton> zoomButtons = zoomBox.Controls.OfType<RadioButton>().ToList();
            foreach(var button in zoomButtons)
            {
                if (button.Checked)
                {
                    //если радиокнопка выбрана - ее значение присваивается переменной zoom
                    zoom = int.Parse(button.Text.Substring(1));
                }
            }
            return zoom;
        }

        //метод получения текущего способа увеличения
        //возвращает булеву переменную - будет ли производиться интерполяция изображения, или нет
        private bool GetZoomMethod()
        {
            //если чекбокс выбран, то будет использоваться метод билинейной интерполяции
            if (zoomMethod.Checked) return true;
            //иначе - метод ближайшего соседа
            else return false;
        }

        //метод для определения, будет ли производиться нормирование
        //возвращает булеву переменную - да или нет
        private bool IsNormalized()
        {
            //если чекбокс выбран - яркости пикселей изображения будут нормированы
            if (normalizeBox.Checked) return true;
            //иначе - не будут
            else return false;
        }

        //обработчик нажатия на кнопку загрузки изображения
        //вызывает метод открытия окна Проводника
        //параметр sender - объект, вызвавший событие (кнопка loadButton)
        //параметр e - объект, содержащий данные о событии 
        private void LoadButton_Click(object sender, EventArgs e)
        {
            //удаление объекта класса, содержащего увеличенный фрагмент, если он был выбран
            zoomImage = null;
            isZoomChosen = false;
            //зум по умолчанию увеличивает фрагмент в 3 раза
            zoom1Button.Checked = true;
            string fileName = OpenExplorer();
            //в случае, если пользователь выбрал изображение инициализируется
            //объект класса ByteImage с путем файла в качестве параметра конструктора
            if (fileName != "") {
                //при каждом выборе изображения инициализируется новый объект
                image = new ByteImage(fileName);
                int shift = GetCurrentShift();
                //если сдвиг был заранее выбран, он учитывается в построении изображения
                img = image.GetBitmap(shift, highestRow);
                //подстановка изображения и названия файла в окно программы
                imageContainer.Image = img;
                //удаление предыдущего увеличенного фрагмента, если он был выбран
                zoomContainer.Image = null;
                //построение обзорного изображения по изображению в формате Bitmap
                overviewContainer.Image = image.BuildOverviewImage(shift);
                lineFileName.Text = fileName.Substring(fileName.LastIndexOf(@"\") + 1);
                //текст с подсказкой становится видимым
                altText.Visible = true;
            }
        }

        //обработчик выбора первой радиокнопки со сдвигом 0
        //вызывает метод перестройки изображения, если оно уже выбрано пользователем
        //параметр sender - объект, вызвавший событие (радиокнопка radioShift1)
        //параметр e - объект, содержащий данные о событии 
        private void RadioShift1_CheckedChanged(object sender, EventArgs e)
        {
            if (image != null)
            {
                //передает в метод построения изображения сдвиг 0
                img = image.GetBitmap(0, highestRow);
                imageContainer.Image = img;
                //построение обзорного изображения при изменении сдвига кодов
                overviewContainer.Image = image.BuildOverviewImage(0);
                //если фрагмент для увеличения выбран, он перестраивается с новым сдвигом
                if (isZoomChosen)
                {
                    BuildZoomedImage(part);
                }
            }
        }

        //обработчик выбора второй радиокнопки со сдвигом 1
        //вызывает метод перестройки изображения, если оно уже выбрано пользователем
        //параметр sender - объект, вызвавший событие (радиокнопка radioShift2)
        //параметр e - объект, содержащий данные о событии 
        private void RadioShift2_CheckedChanged(object sender, EventArgs e)
        {
            if (image != null)
            {
                img = image.GetBitmap(1, highestRow);
                imageContainer.Image = img;
                overviewContainer.Image = image.BuildOverviewImage(1);
                if (isZoomChosen)
                {
                    BuildZoomedImage(part);
                }
            }
        }

        //обработчик выбора третьей радиокнопки со сдвигом 2
        //вызывает метод перестройки изображения, если оно уже выбрано пользователем
        //параметр sender - объект, вызвавший событие (радиокнопка radioShift3)
        //параметр e - объект, содержащий данные о событии 
        private void RadioShift3_CheckedChanged(object sender, EventArgs e)
        {
            if (image != null)
            {
                img = image.GetBitmap(2, highestRow);
                imageContainer.Image = img;
                overviewContainer.Image = image.BuildOverviewImage(2);
                if (isZoomChosen)
                {
                    BuildZoomedImage(part);
                }
            }
        }

        //обработчик изменения текста в текстовом поле, принимающем величину шага прокрутки
        //параметр sender - объект, вызвавший событие (текстовое поле scrollStepInput)
        //параметр e - объект, содержащий данные о событии 
        private void ScrollStepInput_TextChanged(object sender, EventArgs e)
        {
            int scrollStep;
            //если пользователь ввел целое число, большее нуля, устанавливается шаг прокрутки
            if(int.TryParse(scrollStepInput.Text, out scrollStep) && scrollStep > 0)
            {
                imagePanel.VerticalScroll.SmallChange = scrollStep;
            }
            else if(scrollStepInput.Text != "")
            {
                MessageBox.Show("Введите корректное значение - целое положительное число", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                scrollStepInput.Text = "";
            }
        }

        //обработчик нажатия на кнопку для выбора фрагмента изображения
        //кнопка находится в одной группе с полем ввода верхнего ряда highestRowInput
        //вызывает метод, принимающий сдвиг и верхний ряд для перестройки изображения
        //параметр sender - объект, вызвавший событие (кнопка highestRowButton)
        //параметр e - объект, содержащий данные о событии 
        private void HighestRowButton_Click(object sender, EventArgs e)
        {
            int highestY;
            //принимает текущий выбранный сдвиг
            int shift = GetCurrentShift();
            //если введено целое положительное число и выбрано изображение
            if (int.TryParse(highestRowInput.Text, out highestY) && highestY >= 0 && image != null)
            {
                //проверка величины верхней строки, введенной пользователем
                //если величина превышает высоту изображения, пользователь получит об этом предупреждение
                if (highestY >= image.Height)
                {
                    MessageBox.Show("Слишком большая величина для обрезки изображения", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    highestRowInput.Text = "";
                }
                else
                {
                    //построение изображения с учетом верхнего ряда
                    img = image.GetBitmap(shift, highestY);
                    //сохраняем выбранный верхний ряд в переменной
                    highestRow = highestY;
                    imageContainer.Image = img;
                    //если высота отображаемого изображения меньше 60, фрагмент для увеличения тоже уменьшается
                    width = (image.Height - highestRow < 60) ? image.Height - highestRow : 60;
                    height = width;
                }
            }
            else if(highestRowInput.Text != "")
            {
                MessageBox.Show("Введите корректное значение - целое положительное число", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                highestRowInput.Text = "";
            }
        }

        //метод для получения яркости пиксела, находящегося в точке point
        //возвращает яркость в диапазоне от 0 до 1023
        private ushort GetColorByPixel(Point point)
        {
            //получение яркости из объекта класса ByteImage, содержащего информацию об изображении
            return image.GetColorByPixel(point);
        }

        //обработчик передвижения курсора по изображению
        //параметр sender - объект, вызвавший событие (текстовое поле scrollStepInput)
        //параметр e - объект, содержащий данные о событии
        private void ImageContainer_MouseMove(object sender, MouseEventArgs e)
        {
            //если выбрано изображение и курсор находится в его пределах
            //передает информацию о координатах и цвете в текстовые поля формы:
            //x - xValue, y - yValue, яркость - brightValue
            if(image != null && e.Location.X < img.Width)
            {
                brightValue.Text = GetColorByPixel(e.Location).ToString();
                xValue.Text = e.Location.X.ToString();
                yValue.Text = (e.Location.Y + highestRow).ToString();
            }   
        }

        //метод для определения увеличиваемого фрагмента изображения
        //принимает точку на изображении, которую выбрал пользователь
        //возвращает верхнюю левую точку, по которой можно выделить нужный фрагмент
        private Point GetZoomedArea(Point point)
        {
            //создание новой точки - для хранения верхнего левого угла изображения
            Point startPoint = new Point();
            //нахождение X и Y этой точки зависит от центральной точки фрагмента, выбранной пользователем
            startPoint.X = Math.Min(Math.Max(point.X - width / 2, 0), image.Width - width);
            startPoint.Y = Math.Min(Math.Max(point.Y + highestRow - height / 2, highestRow), image.Height - height);
            return startPoint;
        }

        //метод для построения увеличенного изображения
        //принимает в качестве параметра фрагмент в масштабе 1:1 в виде одномерного массива
        private void BuildZoomedImage(ushort[] part)
        {
            //переменная, получающая текущий зум, выбранный пользователем
            int zoom = GetZoomValue();
            //флаг, показывающий, будет ли использоваться билинейная интерполяция
            bool bilinear = GetZoomMethod();
            //флаг, показывающий, будет ли изображение нормировано
            bool isNormalized = IsNormalized();
            //если увеличиваемый фрагмент слишком мал, билинейная интерполяция и нормирование не используются
            if (part.Length <= 1)
            {
                bilinear = false;
                isNormalized = false;
            }
            int shift = GetCurrentShift();
            zoomImage = new ZoomImage(part, width, zoom, shift);
            //построение изображения с помощью объекта класса ZoomImage и вывод на форму
            zoomContainer.Image = zoomImage.GetBitmap(isNormalized, bilinear);
        }

        //метод для получения массива яркостей пикселов фрагмента изображения для увеличения
        //принимает левую верхнюю точку фрагмента
        //возвращает одномерный массив чисел типа ushort в диапазоне от 0 до 1023
        private ushort[] GetPartByPoint(Point point)
        {
            ushort[] temp = new ushort[width * height];
            int index = 0;
            for (int i = point.Y; i < point.Y + height; i++)
            {
                for (int j = point.X; j < point.X + width; j++)
                {
                    temp[index] = image.originalPixels[i * image.Width + j];
                    index++;
                }
            }
            return temp;
        }

        //обработчик нажатия на изображение, увеличивает выбранный пользователем фрагмент
        //параметр sender - объект, вызвавший событие (поле с изображением imageContainer)
        //параметр e - объект, содержащий данные о событии
        private void ImageContainer_Click(object sender, MouseEventArgs e)
        {
            //флаг, определяющий, выбран ли фрагмент для масштабирования
            isZoomChosen = true;
            //получение фрагмента изображения
            part = GetPartByPoint(GetZoomedArea(e.Location));
            BuildZoomedImage(part);
            //текст с подсказкой становится невидимым
            altText.Visible = false;
        }

        //обработчик нажатия на чекбокс "Интерполировать"
        //при изменении в соответствии с выбранным методом переделывает увеличенный фрагмент
        //параметр sender - объект, вызвавший событие (чекбокс zoomMethod)
        //параметр e - объект, содержащий данные о событии
        private void ZoomMethod_CheckedChanged(object sender, EventArgs e)
        {
            //если фрагмент для масштабирования выбран
            if (isZoomChosen)
            {
                BuildZoomedImage(part);
            }
        }

        //обработчик нажатия на чекбокс "Нормировать"
        //при изменении перестраивает изображение, либо с нормированием, либо без
        //параметр sender - объект, вызвавший событие (чекбокс normalizeBox)
        //параметр e - объект, содержащий данные о событии
        private void NormalizeBox_CheckedChanged(object sender, EventArgs e)
        {
            if (isZoomChosen)
            {
                BuildZoomedImage(part);
            }
        }

        //обработчик нажатия на радиокнопку zoom1Button
        //перестраивает изображение с выбранным увеличением - в 3 раза
        //параметр sender - объект, вызвавший событие (радиокнопка zoom1Button)
        //параметр e - объект, содержащий данные о событии
        private void Zoom1Button_CheckedChanged(object sender, EventArgs e)
        {
            //если выбран фрагмент для увеличения
            if (isZoomChosen)
            {
                BuildZoomedImage(part);
            }
        }

        //обработчик нажатия на радиокнопку zoom2Button
        //перестраивает изображение с выбранным увеличением - в 5 раз
        //параметр sender - объект, вызвавший событие (радиокнопка zoom2Button)
        //параметр e - объект, содержащий данные о событии
        private void Zoom2Button_CheckedChanged(object sender, EventArgs e)
        {
            if (isZoomChosen)
            {
                BuildZoomedImage(part);
            }
        }

        //обработчик нажатия на радиокнопку zoom3Button
        //перестраивает изображение с выбранным увеличением - в 7 раз
        //параметр sender - объект, вызвавший событие (радиокнопка zoom3Button)
        //параметр e - объект, содержащий данные о событии
        private void Zoom3Button_CheckedChanged(object sender, EventArgs e)
        {
            if (isZoomChosen)
            {
                BuildZoomedImage(part);
            }
        }
    }
}
