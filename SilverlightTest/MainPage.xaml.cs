﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverlightTest
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var stream = new MemoryStream();

            double v1 = 1.0;
            double v2 = 0.00000024312;
            double v3 = 38423423434.434;
            double v4 = .0;

            int loops = 10000;

            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var sw = Stopwatch.StartNew();

                for (int i = 0; i < loops; ++i)
                {
                    NetSerializer.Primitives.WritePrimitive(stream, v1);
                    NetSerializer.Primitives.WritePrimitive(stream, v2);
                    NetSerializer.Primitives.WritePrimitive(stream, v3);
                    NetSerializer.Primitives.WritePrimitive(stream, v4);
                }

                sw.Stop();

                Console.WriteLine("Writing {0} ms", sw.ElapsedMilliseconds);
            }

            long size = stream.Position;

            Console.WriteLine("Size {0}", size);

            stream.Position = 0;

            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var sw = Stopwatch.StartNew();

                for (int i = 0; i < loops; ++i)
                {
                    NetSerializer.Primitives.ReadPrimitive(stream, out v1);
                    NetSerializer.Primitives.ReadPrimitive(stream, out v2);
                    NetSerializer.Primitives.ReadPrimitive(stream, out v3);
                    NetSerializer.Primitives.ReadPrimitive(stream, out v4);
                }

                sw.Stop();

                Console.WriteLine("Reading {0} ms", sw.ElapsedMilliseconds);
            }
        }

        public class Test
        {
            public int a { get; set; }
            public string b { get; set; }
            public int? c { get; set; }
            public DateTime d { get; set; }
            public DateTime? e { get; set; }
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            NetSerializer.Serializer.Initialize(new[] {typeof (Test)});

            var t = new Test() {a = 99, b = "hallo", c = 88, d = new DateTime(2013, 5, 6), e = null};

            var s = new MemoryStream();

            NetSerializer.Serializer.Serialize(s, t);

            var ret = (Test) NetSerializer.Serializer.Deserialize(s);

            output.Items.Add(".a == .a : " + (t.a == ret.a).ToString());
            output.Items.Add(".b == .b : " + (t.b == ret.b).ToString());
            output.Items.Add(".c == .c : " + (t.c == ret.c).ToString());
            output.Items.Add(".d == .d : " + (t.d == ret.d).ToString());
            output.Items.Add(".e == .e : " + (t.e == ret.e).ToString());
        }
    }
}
