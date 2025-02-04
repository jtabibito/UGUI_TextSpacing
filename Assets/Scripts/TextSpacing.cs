﻿using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class TextSpacing : BaseMeshEffect
{

    enum HorizontalAlignment
    {
        Left,
        Center,
        Right
    }


    public class Line
    {
        public int startVertexIndex;
        public int endVertexIndex;
        public int vertexCount;

        public Line(int startIndex, int charCount)
        {
            startVertexIndex = startIndex;
            vertexCount = 6*charCount; // 每个字符占6个顶点
            endVertexIndex = startVertexIndex + vertexCount - 1;
        }

    }

    [Tooltip("This value set 1 equals 1/100em of the font size.")]
    public float charSpacing = 0.0f;

    public bool Regexable = true;

    // 匹配所有标签: <.*?>
    // 匹配所有标签: <[a-z]+?\=.+?>|<\w+?>|<\/[a-z]+?>
    // 匹配所有size标签: (<size=[0-9]+>.*?</size>)+?
    // 匹配所有size标签里面的内容(过滤size标签): ((?<=<size=[0-9]+>).*?(?=</size>))+?

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
        {
            return;
        }
        
        
        var text = GetComponent<Text>();
        if (text == null)
        {
            return;
        }


        HorizontalAlignment alignment;
        if (text.alignment == TextAnchor.MiddleLeft || text.alignment == TextAnchor.LowerLeft || text.alignment == TextAnchor.UpperLeft)
        {
            alignment = HorizontalAlignment.Left;
        }
        else if (text.alignment == TextAnchor.MiddleCenter || text.alignment == TextAnchor.LowerCenter || text.alignment == TextAnchor.UpperCenter)
        {
            alignment = HorizontalAlignment.Center;
        }
        else
        {
            alignment = HorizontalAlignment.Right;
        }

        List<UIVertex> vertices = new List<UIVertex>();
        vh.GetUIVertexStream(vertices);

        // 如果选择正则匹配,则过滤标签
        string replaced = Regexable ? Regex.Replace(text.text, @"<[a-z]+?\=.+?>|<\w+?>|<\/[a-z]+?>", string.Empty) : text.text;
        string[] textLines = replaced.Split('\n');

        List<int> lTextLines = new List<int>();
        for (int i = 0; i < textLines.Length; ++i)
        {
            if (text.horizontalOverflow == HorizontalWrapMode.Wrap)
            {
                int charsWidth = 0;
                int startIndex = 0;
                int twidth = (int)text.rectTransform.rect.width;
                for (int j = 0, length = textLines[i].Length; j < length; ++j)
                {
                    if (text.font.GetCharacterInfo(textLines[i][j], out CharacterInfo charInfo, text.fontSize, text.fontStyle))
                    {
                        charsWidth += charInfo.advance;
                        if (charsWidth > twidth)
                        {
                            lTextLines.Add(j-startIndex);
                            startIndex = j;
                            charsWidth = charInfo.advance;
                        }
                    }
                }
                if (charsWidth > 0)
                {
                    lTextLines.Add(textLines[i].Length - startIndex);
                }
            }
            else
            {
                lTextLines.Add(textLines[i].Length);
            }
        }

        Line[] lines = new Line[lTextLines.Count];

        for (int i = 0; i < lines.Length; ++i)
        {
            if (i == 0)
            {
                lines[i] = new Line(0, lTextLines[i]);
            }
            else if (i+1 == lines.Length)
            {
                lines[i] = new Line(lines[i-1].endVertexIndex + 1, lTextLines[i]);
            }
            else
            {
                lines[i] = new Line(lines[i-1].endVertexIndex + 1, lTextLines[i]);
            }
        }
        
        float realCharSpacing = charSpacing*text.fontSize/100;

        for (int i = 0; i < lines.Length; ++i)
        {
            int vertexCount = lines[i].vertexCount;
            
            for (int j = lines[i].startVertexIndex; j <= lines[i].endVertexIndex; ++j)
            {
                if (j < 0 || j >= vertices.Count)
                {
                    continue;
                }
                
                UIVertex vertex = vertices[j];

                if (alignment == HorizontalAlignment.Left)
                {
                    vertex.position.x += realCharSpacing * ((j - lines[i].startVertexIndex)/6);
                }
                else if (alignment == HorizontalAlignment.Center)
                {
                    float offset = vertexCount/6 % 2 == 0 ? 0.5f : 0;
                    vertex.position.x += realCharSpacing * ((j - lines[i].startVertexIndex)/6 - vertexCount/12 + offset);
                }
                else
                {
                    vertex.position.x += realCharSpacing * ((j - lines[i].startVertexIndex)/6 - vertexCount/6 + 1);
                }

                vertices[j] = vertex;
                if (j%6 <= 2)
                {
                    vh.SetUIVertex(vertex, j/6*4 + j%6);
                }
                else if (j%6 == 4)
                {
                    vh.SetUIVertex(vertex, j/6*4 + j%6 - 1);
                }
            }
        }
    }
}
