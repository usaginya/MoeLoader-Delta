using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoeLoader
{
    /// <summary>
    /// 管理浏览过的图片id
    /// </summary>
    public class ViewedID
    {
        /// <summary>
        /// 曾经浏览的id范围集合
        /// </summary>
        private List<IdRange> viewedIds = new List<IdRange>();

        /// <summary>
        /// 本次浏览的id集合
        /// </summary>
        private List<int> viewingIds = new List<int>();

        /// <summary>
        /// 曾经浏览过的最大的id
        /// </summary>
        private int viewedBiggestId;

        /// <summary>
        /// 是否曾经浏览过
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsViewed(int id)
        {
            foreach (IdRange r in viewedIds)
            {
                if (r.Contains(id))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 曾经浏览过的最大的id
        /// </summary>
        public int ViewedBiggestId
        {
            get { return viewedBiggestId; }
            //set { viewedBiggestId = value; }
        }

        /// <summary>
        /// 本次正在浏览的id
        /// </summary>
        /// <param name="id"></param>
        public void AddViewingId(int id)
        {
            viewingIds.Add(id);
        }

        /// <summary>
        /// 从字符串加载已浏览过的id
        /// </summary>
        /// <param name="rangeStr"></param>
        public void AddViewedRange(string rangeStr)
        {
            //16311....16317,16320....16340
            //编号,范围长度,... 16311,6;3,20;
            if (rangeStr.Contains(','))
            {
                string[] parts = rangeStr.Split(';');
                int last = -1;
                foreach (string part in parts)
                {
                    int sp = part.IndexOf(',');
                    int start = int.Parse(part.Substring(0, sp));
                    int range = int.Parse(part.Substring(sp + 1));

                    if (last > -1)
                    {
                        start += last;
                    }
                    viewedIds.Add(new IdRange(start, range, last == -1));
                    last = start + range;
                    //for (int i = 0; i <= range; i++)
                    //{
                        //viewedIds.Add(start + i);
                    //viewedIds.Add(new IdRange(start, range));

                    if (viewedBiggestId < start + range)
                        viewedBiggestId = start + range;
                    //}
                }
            }
            else if (rangeStr.Length > 0)
            {
                //向前兼容配置文件
                int id = int.Parse(rangeStr);
                viewedIds.Add(new IdRange(0, id, true));
                viewedBiggestId = id;
            }
        }

        /// <summary>
        /// 使用游程编码压缩进行存储
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            //编号,范围长度,... 16311,6;8,20;
            List<int> temp = new List<int>();
            IdRange firstPart = new IdRange(0, 0, true);
            //融合并排序
            //temp.AddRange(viewedIds);
            foreach (IdRange idr in viewedIds)
            {
                if (!idr.isFirst)
                    temp.AddRange(idr.ToList());
                else firstPart = idr;
            }
            temp.AddRange(viewingIds);
            temp.Sort();

            //string firstPartStr = firstPart.Start + "," + firstPart.Range + ";";

            //压缩
            if (temp.Count > 1)
            {
                //超过该数量时向第一部分合并
                const int MAX_ID = 1000;
                int startIndex = 0;
                if (temp.Count > MAX_ID)
                {
                    //大于上限时归并至第一部分
                    firstPart.Range = temp[temp.Count - MAX_ID - 1];
                    startIndex = temp.Count - MAX_ID;
                }

                StringBuilder sb = new StringBuilder();
                //int last = temp[0], range = 0, lastTrim = 0;
                int last = temp[startIndex], range = 0, lastTrim = firstPart.Range;
                //for (int i = 1; i < temp.Count; i++)
                for (int i = startIndex + 1; i < temp.Count; i++)
                {
                    if (i < temp.Count - 1)
                    {
                        //跳过重复的
                        if (temp[i] == temp[i + 1])
                            continue;
                    }

                    if (temp[i] == last + range + 1)
                    {
                        range++;
                        //continue;
                    }
                    else if (temp[i] != last)
                    {
                        //遇到不连续点且非重复
                        sb.Append((last - lastTrim) + "," + range + ";");
                        lastTrim = last + range;
                        last = temp[i];
                        range = 0;
                    }
                }

                return firstPart.Start + "," + firstPart.Range + ";" + sb.ToString() + (last - lastTrim) + "," + range;
            }
            else if (temp.Count == 1)
            {
                return firstPart.Start + "," + firstPart.Range + ";" + (temp[0] - firstPart.Range) + ",0";
            }
            else return firstPart.Start + "," + firstPart.Range;
        }

        ///////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 一个范围
        /// </summary>
        private class IdRange
        {
            public int Start;
            public int Range;

            /// <summary>
            /// 是否第一部分（较长）
            /// </summary>
            public bool isFirst;

            /// <summary>
            /// 在此范围内
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public bool Contains(int id)
            {
                if (id >= Start && id <= Start + Range)
                    return true;
                else return false;
            }

            public IdRange(int start, int range, bool isFirst)
            {
                this.Start = start;
                this.Range = range;
                this.isFirst = isFirst;
            }

            public int[] ToList()
            {
                int[] re = new int[Range + 1];
                for (int i = 0; i <= Range; i++)
                {
                    re[i] = Start + i;
                }
                return re;
            }
        }
    }
}
