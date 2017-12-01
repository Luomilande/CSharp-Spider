function RequestByPostMethod(RequestUrl, PostObject, Callback) {
    //异步传输数据功能
    $.ajax({
        type: "POST", //数据传输方式：POST/GET
        url: RequestUrl, //数据提交到哪个地方
        contentType: "application/json; charset=utf-8", //HTTP数据流的类型
        data: JSON.stringify(PostObject), //需提交的数据内容
        dataType: "json", //数据内容的格式
        success: function (data) {
            //数据提交成功的事件
            Callback(data); //参数Callback为回调函数
        },
        error: function (data) {
            //数据提交失败的事件
            alert('fail');
        }
    });
}