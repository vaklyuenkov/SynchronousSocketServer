function myFunction(clicked_id) 
{
    var ppid = clicked_id.toString();
    var goal_id = "myPopup" + ppid;
    var popup = document.getElementById(goal_id);
    popup.classList.toggle("show");
}