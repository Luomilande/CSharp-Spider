$("#enterUrl").click(function () {
    var url = $("#webUrl").val();
    var obj = new Object();
    obj.url = url;
    RequestByPostMethod("/Home/StartCatch", obj, Startmsg)
})
function Startmsg(data) {
    alert(data.ExecuteResult)
}
$().ready(function () {
    $("#WebList>li").click(function () {
        if ($(this).attr("class") != "disabled")
        {
            var weburl = $(this).text();
            $("#webUrl").val(weburl);
        }
    })
})