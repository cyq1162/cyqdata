using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Json
{
    /// <summary>
    /// 字符状态
    /// </summary>
    internal class CharState
    {
        internal char lastKeywordChar = ' ';
        internal char lastChar = ' ';
        /// <summary>
        /// 是否格式格式【true属性必须双引号，false属性可以单引号和无引号。】
        /// </summary>
        internal bool isStrictMode = false;
        public CharState(bool isStrictMode)
        {
            this.isStrictMode = isStrictMode;
        }
        internal bool jsonStart = false;//以 "{"开始了...
        internal bool setDicValue = false;// 可以设置字典值了。
        internal bool escapeChar = false;//以"\"转义符号开始了
        /// <summary>
        /// 数组开始【仅第一开头才算】，值嵌套的以【childrenStart】来标识。
        /// </summary>
        internal bool arrayStart = false;//以"[" 符号开始了
        internal bool childrenStart = false;//子级嵌套开始了。
        /// <summary>
        /// 【-1 未初始化】【0取名阶段】【1 取值阶段】
        /// </summary>
        internal int keyValueState = -1;

        /// <summary>
        /// 【-2 已结束】【-1 未初始化】【0 未开始】【1 无引号开始】【2 单引号开始】【3 双引号开始】
        /// </summary>
        internal int keyStart = -1;
        /// <summary>
        /// 【-2 已结束】【-1 未初始化】【0 未开始】【1 无引号开始】【2 单引号开始】【3 双引号开始】
        /// </summary>
        internal int valueStart = -1;

        internal bool isError = false;//是否语法错误。

        /// <summary>
        /// 只当成一级处理（因为GetLength会递归到每一个子项处理）
        /// </summary>
        internal void CheckIsError(char c)
        {
            switch (c)
            {
                case '\r':
                case '\n':
                case '\t':
                    return;
                case '{'://[{ "[{A}]":[{"[{B}]":3,"m":"C"}]}]
                    isError = jsonStart && keyValueState == 0;//重复开始错误 同时不是值处理。
                    break;
                case '}':
                    isError = !jsonStart || (keyStart > 0 && keyValueState == 0);//重复结束错误 或者 提前结束。
                    if (!isError && isStrictMode)
                    {
                        isError = !((keyStart == 3 && keyValueState == 0) || (valueStart != 2 && keyValueState == 1) || valueStart == -2 || (jsonStart && keyStart == -1));
                    }
                    break;
                case '[':
                    isError = arrayStart && keyValueState == 0;//重复开始错误
                    break;
                case ']':
                    isError = (!arrayStart && valueStart != 3 && keyStart != 3) || (keyValueState == 1 && valueStart == 0);//重复开始错误[{},]1,0  正常：[111,222] 1,1 [111,"22"] 1,-2 
                    break;
                case '"':
                    isError = !jsonStart && !arrayStart;//未开始Json，同时也未开始数组。
                    break;
                case '\'':
                    isError = (!jsonStart && !arrayStart);//未开始Json
                    if (!isError && isStrictMode)
                    {
                        isError = !((keyStart == 3 && keyValueState == 0) || (valueStart == 3 && keyValueState == 1));
                    }
                    break;
                case ':':
                    isError = (!jsonStart && !arrayStart) || (jsonStart && keyStart < 2 && valueStart < 2 && keyValueState == 1);//未开始Json 同时 只能处理在取值之前。
                    break;
                case ',':
                    isError = (!jsonStart && !arrayStart)
                        || (!jsonStart && arrayStart && keyValueState == -1) //[,111]
                        || (jsonStart && keyStart < 2 && valueStart < 2 && keyValueState == 0);//未开始Json 同时 只能处理在取值之后。
                    break;
                //case 't'://true
                //case 'f'://false

                //  break;
                default: //值开头。。
                    isError = (!jsonStart && !arrayStart) || (keyStart == 0 && valueStart == 0 && keyValueState == 0);//
                    if (!isError)
                    {
                        bool isCheckValue = false;
                        if (jsonStart)
                        {
                            if (keyValueState < 1 && keyStart < 2)
                            {
                                //不是引号开头的，只允许字母 {aaa:1}
                                isError = c < 65 || (c > 90 && c < 97) || c > 122;
                                break;
                            }
                            else if (valueStart == 1)
                            {
                                //验证取取值
                                isCheckValue = true;
                            }
                        }
                        else if (arrayStart)
                        {
                            //验证取取值
                            isCheckValue = valueStart < 2;
                        }
                        if (isCheckValue)
                        {
                            switch (c)
                            {
                                case 'r'://true
                                    isError = lastChar != 't';
                                    break;
                                case 'u'://true,null
                                    isError = !((lastKeywordChar == 't' && lastChar == 'r') || (lastKeywordChar == 'n' && lastChar == 'n'));
                                    break;
                                case 'e'://true
                                    isError = !((lastKeywordChar == 't' && lastChar == 'u') || (lastKeywordChar == 'f' && lastChar == 's'));
                                    break;
                                case 'a'://false
                                    isError = lastChar != 'f';
                                    break;
                                case 'l'://false,null 
                                    isError = !((lastKeywordChar == 'f' && lastChar == 'a') || (lastKeywordChar == 'n' && (lastChar == 'u' || lastChar == 'l')));
                                    if (!isError && lastKeywordChar == 'n' && lastChar == 'l')
                                    {
                                        //取消关键字，避免出现 nulllll多个l
                                        lastKeywordChar = ' ';
                                    }
                                    break;
                                case 's'://false
                                    isError = lastChar != 'l';
                                    break;
                                case '.'://数字可以出现小数点，但不能重复出现
                                    isError = keyValueState != 1 || lastKeywordChar == '.';
                                    break;
                                case ' ':
                                    if (lastChar == '.') { isError = true; }
                                    else if (jsonStart && !arrayStart)
                                    {
                                        valueStart = -2;//遇到空格，结束取值。
                                    }
                                    break;
                                default:
                                    //不是引号开头的，只允许数字[1]
                                    isError = c < 48 || c > 57;
                                    break;
                            }
                            //值开头的，只能是：["xxx"] {[{}]
                        }
                    }

                    //if (!isError && keyStart < 2)
                    //{
                    //if ((jsonStart && !arrayStart) && state != 1)
                    //if (jsonStart)//取名阶段
                    //{
                    //    if (keyValueState <= 0)
                    //    {
                    //        //不是引号开头的，只允许字母 {aaa:1}
                    //        isError = isStrictMode || (c < 65 || (c > 90 && c < 97) || c > 122);
                    //    }

                    //}
                    //else if (arrayStart)
                    //{
                    //    if (valueStart < 2)
                    //    {
                    //        switch (c)
                    //        {
                    //            case ' ': break;
                    //            case 'n'://null
                    //                isError = !(lastChar == ' ' || lastChar == '['); break;
                    //            case 'u':
                    //                isError = !(lastChar == 'n' || lastChar == 'r'); break;
                    //            case 'l':
                    //                isError = !(lastChar == 'u' || lastChar == 'a' || lastChar == 'l'); break;
                    //            case 't'://true
                    //                isError = !(lastChar == ' ' || lastChar == '['); break;
                    //            case 'r':

                    //            case 'e':
                    //            case 'f'://false
                    //            case 'a':
                    //            case 's':
                    //                break;
                    //            default:
                    //                //不是引号开头的，只允许数字[1] 空格、null,true,false
                    //                isError = c < 48 || c > 57;
                    //                break;
                    //        }
                    //    }
                    //}
                    //}

                    break;
            }
            if (isError)
            {
                //
            }
        }

        /// <summary>
        /// 设置字符状态(返回true则为关键词，返回false则当为普通字符处理）
        /// 注意：只当成一级处理（因为GetLength会递归到每一个子项处理），所以条件只要考虑一级。
        /// </summary>
        internal bool IsKeyword(char c)
        {
            bool isKeyword = false;
            switch (c)
            {
                case '{'://[{ "[{A}]":[{"[{B}]":3,"m":"C"}]}]{}
                    #region 大括号
                    if (jsonStart)
                    {
                        //{"a":{}} 或者 [{"a":{}}]
                        if (keyValueState == 1 && valueStart < 2)
                        {
                            isKeyword = true;
                            valueStart = 0;
                            childrenStart = true;
                        }
                    }
                    else
                    {
                        //{} 或者 [{},{}]
                        if (keyValueState == -1 || (arrayStart && keyValueState == 0 && lastKeywordChar == ','))
                        {
                            isKeyword = true;
                            jsonStart = true;
                            keyValueState = 0;
                        }
                    }
                    //if (keyStart <= 0 && valueStart <= 0)
                    //{
                    //    if (jsonStart && keyValueState == 1)
                    //    {
                    //        valueStart = 0;
                    //        childrenStart = true;
                    //    }
                    //    else
                    //    {
                    //        keyValueState = 0;
                    //    }
                    //    jsonStart = true;//开始。
                    //    isKeyword = true;
                    //}
                    #endregion
                    break;
                case '}':
                    if (jsonStart)
                    {
                        #region 大括号结束
                        if (lastChar != '.')
                        {
                            if (keyStart < 1 && valueStart < 2)
                            {
                                isKeyword = true;
                                jsonStart = false;//正常结束。
                                valueStart = -1;
                                keyValueState = 0;
                                setDicValue = true;
                            }
                        }
                    }
                    #endregion
                    break;
                case '[':
                    #region 中括号开始
                    if (jsonStart)
                    {
                        //{"a":[]}
                        if (keyValueState == 1 && valueStart < 1)
                        {
                            isKeyword = true;
                            childrenStart = true;
                        }
                    }
                    else
                    {
                        //[{}]
                        if (keyValueState == -1)
                        {
                            arrayStart = true;
                            isKeyword = true;
                        }
                    }
                    #endregion
                    break;
                case ']':
                    if (arrayStart)
                    {
                        #region 中括号结束
                        if (lastChar != '.')
                        {
                            if (!jsonStart && (keyStart < 1 && valueStart < 1) || (keyStart == -1 && valueStart == 1))
                            {
                                //不支持数组嵌套
                                arrayStart = false;
                                isKeyword = true;
                            }
                        }
                    }
                    #endregion
                    break;
                case '"':
                case '\'':
                    // CheckIsError(c);
                    if (isStrictMode && c == '\'')
                    {
                        break;
                    }
                    #region 引号
                    if (jsonStart || arrayStart)
                    {
                        if (!jsonStart && arrayStart)
                        {
                            keyValueState = 1;//如果是数组，只有取值，没有Key，所以直接跳过0
                        }
                        if (keyValueState == 0)//key阶段
                        {
                            keyStart = (keyStart < 1 ? (c == '"' ? 3 : 2) : -2);
                            isKeyword = true;
                        }
                        else if (keyValueState == 1)//值阶段
                        {
                            if (valueStart < 1)
                            {
                                valueStart = (c == '"' ? 3 : 2);
                                isKeyword = true;
                            }
                            else if ((valueStart == 2 && c == '\'') || (valueStart == 3 && c == '"'))
                            {
                                if (!escapeChar)
                                {
                                    valueStart = -2;
                                    isKeyword = true;
                                }
                                else
                                {
                                    escapeChar = false;
                                }
                            }

                        }
                    }
                    #endregion
                    break;
                case ':':
                    // CheckIsError(c);
                    #region 冒号
                    if (jsonStart && keyStart < 2 && valueStart < 2 && keyValueState == 0)
                    {
                        keyStart = -2;//0 结束key
                        keyValueState = 1;
                        isKeyword = true;
                    }
                    #endregion
                    break;
                case ',':
                    #region 逗号 {"a": [11,"22", ], "Type": 2.}
                    //{"a":1,"b":"3",...}
                    if (lastChar != '.')
                    {
                        if (jsonStart)
                        {
                            if (keyValueState == 1 && valueStart < 2)// && keyStart < 2 &&  && 
                            {
                                keyValueState = 0;
                                valueStart = 0;
                                setDicValue = true;
                                isKeyword = true;
                            }
                        }
                        else if (arrayStart) //[a,b]  [",",33]
                        {
                            //[{ },{ }]
                            //if ()
                            // || (keyValueState == -1 && valueStart == -1) || (valueStart < 2 && keyValueState == 1)
                            if (keyValueState == 0)
                            {
                                valueStart = 0;
                                isKeyword = true;
                            }
                        }
                    }
                    #endregion
                    break;
                case ' ':
                case '\r':
                case '\n':
                case '\t':
                    if (jsonStart && keyStart < 2 && valueStart < 2)
                    {
                        isKeyword = true;
                        // return true;//跳过空格。
                    }
                    break;
                case 't'://true
                case 'f'://false
                case 'n'://null
                case '-'://-388.8 //负的数字符号
                    if (lastKeywordChar != c && lastKeywordChar != '.')
                    {
                        if (valueStart < 2 && ((arrayStart && !jsonStart && keyStart == -1) || (jsonStart && keyValueState == 1 && valueStart < 1)))
                        {
                            //只改状态，不是关键字
                            valueStart = 1;
                            lastChar = c;
                            lastKeywordChar = c;
                            return false;//直接返回，不检测错误。
                        }
                    }
                    break;
                case '.':
                    if ((jsonStart || arrayStart) && keyValueState == 1 && valueStart == 1 && lastKeywordChar != c)
                    {
                        lastChar = c;
                        lastKeywordChar = c;//记录符号，数字只能有一个符号。
                        return false;//不检测错误。
                    }
                    break;
                default: //值开头。。
                    // CheckIsError(c);
                    if (c == '\\') //转义符号
                    {
                        if (escapeChar)
                        {
                            escapeChar = false;
                        }
                        else
                        {
                            escapeChar = true;
                        }
                    }
                    if (jsonStart)
                    {
                        if (keyStart < 1 && keyValueState < 1)
                        {
                            keyStart = 1;//无引号的
                        }
                        else if (valueStart < 1 && keyValueState == 1)
                        {
                            valueStart = 1;//无引号的
                        }
                    }
                    else if (arrayStart)
                    {
                        keyValueState = 1;
                        if (valueStart < 1)
                        {
                            valueStart = 1;//无引号的
                        }
                    }
                    break;
            }
            if (escapeChar && c != '\\')
            {
                escapeChar = false;
            }
            if (!isKeyword)
            {
                if (isStrictMode)
                {
                    CheckIsError(c);
                }
            }
            else
            {
                lastKeywordChar = c;
            }
            lastChar = c;
            return isKeyword;
        }
    }
}
