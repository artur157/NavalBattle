using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Media;

namespace NavalBattle
{
    public partial class Form1 : Form
    {
        public const int len = 40, widthOfField = len * 22, heightOfField = len * 10, widthOfWindow = widthOfField + 17, heightOfWindow = heightOfField + 40;  // учитываем края

        public static Random rnd = new Random();

        public int[,] my, enemy;   // 0 - пусто, 1 - тут корабль, 2 - стрельнули мимо/тут точно нет, 3 - ранил, 4 - убил
        public List<List<Square>> myShips, enemyShips;   // тут хранятся координаты квадратов кораблей и их состояния

        public bool attack = false;  // false - рандом, true - нацеленно

        public Queue<Point> q = new Queue<Point>();       // очередь на обстрел
        public int iPrev = 0, jPrev = 0, iPrev2, jPrev2;  // координаты 2-х последних обстрелянных квадратов

        public bool strategy = true;   // есть ли у противника интеллект?

        SoundPlayer soundPlayerKill, soundPlayerInjure, soundPlayerMiss;


        public bool rand()      // выдает наудачу true или false
        {
            return rnd.Next(0, 100) % 2 == 0;
        }

        public void swap(ref int a, ref int b){    // обмен значениями переменных
            int tmp = a;
            a = b;
            b = tmp;
        }   

        public bool hasShips(ref int[,] matrix)   // есть ли ещё корабли?
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (matrix[i, j] == 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void clearMatrix(ref int[,] matrix)  // обнуляет матрицу
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    matrix[i,j] = 0;
                }
            }
        }

        public bool shootShip(ref int[,] matrix, ref List<List<Square>> ships, int x, int y){  // true, если подстрелили

            bool result = false;

            for (int i = 0; i < ships.Count; i++)
			{
                bool killed = false;  // убит ли корабль?

                for (int j = 0; j < ships[i].Count; j++)
                {
                    int x_ = ships[i][j].x;  // координаты на поле
                    int y_ = ships[i][j].y;

                    if (x_ == x && y_ == y)
                    {
                        ships[i][j].state = false;   // подбитое состояние
                        result = true;

                        // теперь проверка на ранен/убит
                        killed = true;
                        for (int j2 = 0; j2 < ships[i].Count; j2++)
                        {
                            if (ships[i][j2].state == true) 
                            {
                                killed = false;
                            }
                        }

                        if (strategy && matrix.Equals(my))   // если это враг стреляет по моим кораблям, значит реализуем стратегию
                        {
                            if (attack)   // если до этого уже попадали, то очередь очищаем, другая схема поиска
                            {
                                q.Clear();
                            }

                            if (!attack)   // если первый раз наткнулись, то формируем очередь на обстрел из соседей
                            {
                                // добавляем в очередь соседей, кого можно
                                if (x_ > 0 && my[x_ - 1, y_] < 2)
                                {
                                    q.Enqueue(new Point(x_ - 1, y_));
                                }
                                if (x_ < 9 && my[x_ + 1, y_] < 2)
                                {
                                    q.Enqueue(new Point(x_ + 1, y_));
                                }
                                if (y_ > 0 && my[x_, y_ - 1] < 2)
                                {
                                    q.Enqueue(new Point(x_, y_ - 1));
                                }
                                if (y_ < 9 && my[x_, y_ + 1] < 2)
                                {
                                    q.Enqueue(new Point(x_, y_ + 1));
                                }

                                attack = true;
                            }

                        }
                        
                        // проба на звук
                        if (!killed)
                        {
                            soundPlayerInjure.Play();
                        }

                    }
                }

                if (killed)  // если убит, помечаем соотв цветом
                {
                    soundPlayerKill.Play();

                    for (int j = 0; j < ships[i].Count; j++)
                    {
                        int x_ = ships[i][j].x;
                        int y_ = ships[i][j].y;
                        matrix[x_, y_] = 4;

                        // помечаем состояние = 2 у соседей убитого корабля
                        for (int k = -1; k <= 1; k++)
                        {
                            for (int m = -1; m <= 1; m++)
                            {
                                if (x_ + k >= 0 && x_ + k <= 9 && y_ + m >= 0 && y_ + m <= 9 && matrix[x_ + k, y_ + m] == 0)
                                {
                                    matrix[x_ + k, y_ + m] = 2;
                                }
                            }
                        }
                    }

                    if (strategy && matrix.Equals(my))
                    {
                        attack = false;
                        q.Clear();
                    }
                }
			}

            return result;
        }

        public Form1()
        {
            InitializeComponent();
            this.Width = widthOfWindow;
            this.Height = heightOfWindow;

            // располагаем кнопку
            button1.Width = (int)(1.6 * len);
            button1.Left = (widthOfField - button1.Width) / 2;
            button1.Top = (heightOfField - button1.Height) * 3 / 4;

            // располагаем надпись и радиокнопки
            label1.Left = (widthOfField - label1.Width) / 2;
            label1.Top = (heightOfField - label1.Height) / 4;
            radioButton1.Left = label1.Left + 5;
            radioButton1.Top = label1.Top + label1.Height + 10;
            radioButton2.Left = radioButton1.Left;
            radioButton2.Top = radioButton1.Top + radioButton1.Height + 10;

            // инициализируем массивы и списки
            my = new int[10,10];
            enemy = new int[10, 10];
            myShips = new List<List<Square>>();
            enemyShips = new List<List<Square>>();

            // ставим корабли себе и врагу
            putShips(ref my, ref myShips);
            putShips(ref enemy, ref enemyShips);

            // звуки
            soundPlayerKill = new SoundPlayer();
            soundPlayerKill.Stream = Properties.Resources.kill;
            soundPlayerInjure = new SoundPlayer();
            soundPlayerInjure.Stream = Properties.Resources.injure;
            soundPlayerMiss = new SoundPlayer();
            soundPlayerMiss.Stream = Properties.Resources.miss;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen pen = new Pen(Color.Black);
            Pen thick_pen = new Pen(Color.Black, 2);
            Brush brush = new SolidBrush(Color.Black);
            int offset = widthOfField / 2 + len;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    g.DrawRectangle(pen, i * len, j * len, len, len);  // свой
                    g.DrawRectangle(pen, offset + i * len, j * len, len, len);  // вражеский

                    // своё
                    switch (my[i, j])  
                    {
                        case 0: brush = new SolidBrush(Color.White); break;
                        case 1: brush = new SolidBrush(Color.Green); break;
                        case 2: brush = new SolidBrush(Color.White); break;
                        case 3: brush = new SolidBrush(Color.Blue); break;
                        case 4: brush = new SolidBrush(Color.Red); break;
                    }

                    g.FillRectangle(brush, i * len + 1, j * len + 1, len - 1, len - 1);  // закрасить цветом

                    if (my[i, j] > 1)   // нарисовать крестик
                    {
                        g.DrawLine(thick_pen, i * len, j * len, (i + 1) * len, (j + 1) * len);
                        g.DrawLine(thick_pen, i * len, (j + 1) * len, (i + 1) * len, j * len);
                    }

                    // противника
                    switch (enemy[i, j])  
                    {
                        case 3: brush = new SolidBrush(Color.Blue); break;
                        case 4: brush = new SolidBrush(Color.Red); break;
                        default: brush = new SolidBrush(Color.White); break;
                    }

                    g.FillRectangle(brush, offset + i * len + 1, j * len + 1, len - 1, len - 1);   // закрасить цветом

                    if (enemy[i, j] > 1)   // нарисовать крестик
                    {
                        g.DrawLine(thick_pen, offset + i * len, j * len, offset + (i + 1) * len, (j + 1) * len);
                        g.DrawLine(thick_pen, offset + i * len, (j + 1) * len, offset + (i + 1) * len, j * len);
                    }
                }
            }

        }

        public void putShip(int quan, ref int[,] matrix, ref List<List<Square>> ships)    // ставим корабль с quan палубами
        {
            bool fail = false;

            do   // пробуем
            {
                fail = false;
                int i = rnd.Next(0, 10);
                int j = rnd.Next(0, 10 - quan + 1);

                if (rand())   // ставим по вертикали
                {
                    swap(ref i, ref j);

                    int i1 = i > 0 ? i - 1 : i;  // определим границы проверки
                    int i2 = i + quan - 1 < 9 ? i + quan : i + quan - 1;

                    if (i1 != i && matrix[i1, j] == 1 || i2 != i && matrix[i2, j] == 1)   // проверяем края
                    {
                        fail = true;
                    }

                    for (int k = i1; k <= i2; ++k)  // сначала проверяем, можно ли так
                    {
                        if (matrix[k, j] == 1 || (j < 9 && matrix[k, j + 1] == 1) || (j > 0 && matrix[k, j - 1] == 1))
                        {
                            fail = true;
                            break;
                        }
                    }

                    if (!fail)
                    {
                        List<Square> ship = new List<Square>(); 

                        for (int k = i; k < i + quan; ++k)  // можно => ставим
                        {
                            matrix[k, j] = 1;
                            ship.Add(new Square(k, j));
                        }

                        // поставили, а теперь запомним как корабль
                        ships.Add(ship);
                    }
                }
                else   // ставим по горизонтали
                {
                    int j1 = j > 0 ? j - 1 : j;  // определим границы проверки
                    int j2 = j + quan - 1 < 9 ? j + quan : j + quan - 1;

                    if (j1 != j && matrix[i, j1] == 1 || j2 != j && matrix[i, j2] == 1)   // проверяем края
                    {
                        fail = true;
                    }

                    for (int k = j1; k <= j2; ++k)   // сначала проверяем, можно ли так
                    {
                        if (matrix[i, k] == 1 || (i < 9 && matrix[i + 1, k] == 1) || (i > 0 && matrix[i - 1, k] == 1)) 
                        {
                            fail = true;
                            break;
                        }
                    }

                    if (!fail)
                    {
                        List<Square> ship = new List<Square>(); 

                        for (int k = j; k < j + quan; ++k)  // можно => ставим
                        {
                            matrix[i, k] = 1;
                            ship.Add(new Square(i, k));
                        }

                        // поставили, а теперь запомним как корабль
                        ships.Add(ship);
                    }
                }

            } while (fail);
            
        }

        public void putShips(ref int[,] matrix, ref List<List<Square>> ships)   // расставить корабли по новому
        {
            // убираем все старые корабли
            clearMatrix(ref matrix);
            ships.Clear();

            for (int i = 4; i > 0; --i)   // i - кол-во палуб
            {
                for (int j = 0; j < 5 - i; ++j)  // j - кол-во кораблей
                {
                    putShip(i, ref matrix, ref ships);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)   // ставим корабли по новому
        {
            putShips(ref my, ref myShips);
            Invalidate();
        }

        private void Form1_Click(object sender, EventArgs e)
        {
            int x = Cursor.Position.X - this.Left - 9;
            int y = Cursor.Position.Y - this.Top - 32;
            x -= widthOfField / 2 + len;

            int i = x / len;
            int j = y / len;
            if (x < 0)   // проверяем чтобы не выходили за поле
            {
                i = -1;
            }

            if (i >= 0 && i < 10 && j >= 0 && j < 10)  // если тыкнули врагу
            {
                button1.Visible = false;  // скрыли элементы управления
                label1.Visible = false;
                radioButton1.Visible = false;
                radioButton2.Visible = false;

                if (enemy[i, j] < 2)  // ставим крестик
                {
                    enemy[i, j] += 2;

                    // если i,j есть в матрице кораблей, то пометить состояние и проверить убили ли
                    if (shootShip(ref enemy, ref enemyShips, i, j))
                    {
                        goto L1;
                    }
                    else
                    {
                        soundPlayerMiss.Play();
                    }


L2:                 // теперь ход врага
                    int i_ = 0, j_ = 0;

                    do
                    {
                        i_ = rnd.Next(0, 10);
                        j_ = rnd.Next(0, 10);
                    } while (my[i_, j_] > 1);
                    
                    //////////////////////////////////////////////////////////////ЛОГИКА ПРОТИВНИКА//////////////////////////////////////////////////////////////////////////////////
                    
                    if (attack)  
                    {
                        if (q.Count > 0)   // смотрим вокруг первого подстрелянного квадрата
                        {
                            // смотрим соседей по возможности
                            Point p = q.Dequeue();
                            i_ = p.X;
                            j_ = p.Y;
                        }
                        else    // анализируем 2 последних попадания
                        {
                            if (iPrev == iPrev2)
                            {
                                i_ = iPrev;
                                j_ = Math.Max(jPrev, jPrev2) + 1;

                                if (j_ < 0 || j_ > 9 || my[i_, j_] > 1)
                                {
                                    if (j_ < 0 || j_ > 9 || my[i_, j_] == 3)  // причем если он подбитый, то идем в ту сторону дальше
                                    {
                                        j_ = Math.Max(jPrev, jPrev2) + 2;
                                    }
                                    
                                    if (j_ < 0 || j_ > 9 || my[i_, j_] > 1)
                                    {
                                        j_ = Math.Min(jPrev, jPrev2) - 1;

                                        if (j_ < 0 || j_ > 9 || my[i_, j_] == 3)  // причем если он подбитый, то идем в ту сторону дальше
                                        {
                                            j_ = Math.Min(jPrev, jPrev2) - 2;
                                        }
                                    }
                                }
                            }
                            else if (jPrev == jPrev2)
                            {
                                j_ = jPrev;
                                i_ = Math.Max(iPrev, iPrev2) + 1;

                                if (i_ < 0 || i_ > 9 || my[i_, j_] > 1)
                                {
                                    if (i_ < 0 || i_ > 9 || my[i_, j_] == 3)  // причем если он подбитый, то идем в ту сторону дальше
                                    {
                                        i_ = Math.Max(iPrev, iPrev2) + 2;
                                    }

                                    if (i_ < 0 || i_ > 9 || my[i_, j_] > 1)
                                    {
                                        i_ = Math.Min(iPrev, iPrev2) - 1;

                                        if (i_ < 0 || i_ > 9 || my[i_, j_] == 3)  // причем если он подбитый, то идем в ту сторону дальше
                                        {
                                            i_ = Math.Min(iPrev, iPrev2) - 2;
                                        }
                                    }
                                }
                            }
                        }
                        
                    }


                    my[i_, j_] += 2;   // стрельнули


                    if (my[i_, j_] == 3) // если ранили, запоминаем координаты 2-х последних выстрелов
                    {
                        iPrev2 = iPrev;
                        jPrev2 = jPrev;
                        iPrev = i_;   
                        jPrev = j_;
                    }

                    // если i_,j_ есть в матрице кораблей, то пометить состояние и проверить убили ли
                    if (shootShip(ref my, ref myShips, i_, j_))
                    {
                        Refresh();
                        Thread.Sleep(300);
                        if (!hasShips(ref my))   // проверяем, если нужно на выход  
                        {
                            goto L1;
                        }
                        else
                        {
                            goto L2;
                        }
                    }
                    else
                    {
                        soundPlayerMiss.Play();
                    }

                    //////////////////////////////////////////////////////////////ЛОГИКА ПРОТИВНИКА//////////////////////////////////////////////////////////////////////////////////

L1:                 Invalidate();

                    // проверка на окончание игры
                    if (!hasShips(ref enemy) && !hasShips(ref my))
                    {
                        MessageBox.Show("НИЧЬЯ");
                        Application.Exit();
                    }
                    else if (!hasShips(ref enemy))
                    {
                        MessageBox.Show("ПОБЕДА");
                        Application.Exit();
                    }
                    else if (!hasShips(ref my))
                    {
                        MessageBox.Show("ПОРАЖЕНИЕ");
                        Application.Exit();
                    }
                }

                
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            strategy = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            strategy = false;
        }
    }

    public class Square{   // точка на поле
        public int x, y;          // координаты
        public bool state = true; // true, если неподбита

        public Square(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
