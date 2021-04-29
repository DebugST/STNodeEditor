$(document).ready(function(){
    $(".anchor_btn").click(function(){
        var nTop = $(".anchor_point[anchor='" + $(this).attr('anchor') + "']").offset().top;
        if(!$(this).hasClass("a_node_root")) nTop -= 5;
        $('html,body').animate({scrollTop:nTop},500);
    });
    $(window).scroll(function(){
        var nMin = 100000,strName = '',strLast = '';
        var nHeight =window.innerHeight;// document.body.clientHeight;
        var nHtmlTop = $(this).scrollTop();
        var es = $('.anchor_point');
        for(i = 0; i < es.length; i++){
            var nSub = Math.abs(es[i].offsetTop - nHtmlTop);
            if(nSub < nMin){
                nMin = nSub;
                if(nHtmlTop + (nHeight / 2) >= es[i].offsetTop){
                    strName = $(es[i]).attr('anchor');
                }
            }
            strLast = $(es[i]).attr('anchor');
        }
        $(".anchor_btn").removeClass('active');
        $(".anchor_btn[anchor='" + strName + "']").addClass('active');
    });
    
    $(document).on("touchstart","#div_left",function(e){});
    
    if(navigator.userAgent.toLowerCase().indexOf('webkit') == -1){
        console.log('what the fuck...!!!!!!');
        $('body').append("<div id='div_fuck_the_kernel'>"
            + "都TM2021年了 Windows还在采用可视滚动条 然而只有WebKit提供了滚动条样式的支持"
            + "</div>"
        );
        var e = $('#div_fuck_the_kernel');
        e.css({
            position:"fixed",
            top:0,
            left:0,
            right:0,
            color:"white",
            "line-height":"20px",
            "text-align":"center",
            "background-color":"rgba(255,255,0,.5)",
            border:"solid 1px yellow",
            "z-index":100
        });
        e.click(function(){e.remove();});
    }
});