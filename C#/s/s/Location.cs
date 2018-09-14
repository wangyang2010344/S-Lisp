using System;
using System.Collections.Generic;
using System.Text;

namespace s
{
    public class Location
    {
        private int row, col, i;
        public Location(int row, int col, int i)
        {
            this.row = row;
            this.col = col;
            this.i = i;
        }
        public int Row()
        {
            return row;
        }
        public int Col()
        {
            return col;
        }
        public int Index()
        {
            return i;
        }

        public override string ToString()
        {
            return "λ��" + (row+1) + "��" + (col+1) + "�У���" + (i+1) + "���ַ���";
        }
    }
}
