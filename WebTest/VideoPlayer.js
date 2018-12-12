
(function(window, document){
    var video = document.getElementsByTagName("video")[0],
    videoControls = document.getElementById('videoControls'),
    play = document.getElementById('play'),
    
    progressContainer = document.getElementById("progress"),
    progressHolder = document.getElementById("progress_box"),
    playProgressBar = document.getElementById("play_progress"),

    fullScreenToggleButton = document.getElementById("fullScreen");
    var videoPlayer={
        init:function()
        {
            var that = this;
            video.removeAttribute('controls');
            video.addEventListener("loadeddata", this.initializeControls, false);
            this.handleButtonPresses();
            this.videoScrubbing();
        },
        initializeControls:function()
        {
            videoPlayer.showHideControls();
        },
        showHideControls:function()
        {
            video.addEventListener('mouseover', function(){
                videoControls.style.opacity = 1;
            }, false);
            videoControls.addEventListener('mouseover', function(){
                videoControls.style.opacity = 1;
            }, false);
            video.addEventListener('mouseout', function(){
                videoControls.style.opacity = 0;
            }, false);
            videoControls.addEventListener('mouseout', function(){
                videoControls.style.opacity = 0;
            }, false);
        },
        handleButtonPresses:function(){
            video.addEventListener('click', this.playPause, false);
            play.addEventListener('click', this.playPause, false);
            video.addEventListener('play', function(){
                play.title = "Pause";
                play.innerHTML='<span id="pauseButton">&#x2590;&#x2590;</span>';

                videoPlayer.trackPlayProgress();
            }, false);
            video.addEventListener('pause', function(){
                play.title = 'play';
                play.innerHTML = '&#x25BA;';

                videoPlayer.stopTrackingPlayProgress();
            }, false);
            video.addEventListener('ended', function(){
                this.currentTime = 0;
                this.pause();
            }, false);
        },
        trackPlayProgress:function()
        {
            (function progressTrack(){
                videoPlayer.updatePlayProgress();
                playProgressInterval = setTimeout(progressTrack, 50);
            })();
        },
        updatePlayProgress : function(){ 
            playProgressBar.style.width = ((video.currentTime/video.duration) * (progressHolder.offsetWidth)) + "px";
        },
        stopTrackingPlayProgress : function(){ 
            clearTimeout( playProgressInterval ); 
        },
        playPause:function(){
            if(video.paused || video.ended)
            {
                if(video.ended)
                {
                    video.currentTime = 0;
                }
                video.play();
            }
            else
            {
                video.pause();
            }
        },
        videoScrubbing:function(){
            progressHolder.addEventListener("mousedown", function(){
                videoPlayer.stopTrackingPlayProgress();
                videoPlayer.playPause();
                document.onmousemove = function(e){
                    videoPlayer.setPlayProgress( e.pageX );
                }
                progressHolder.onmouseup=function(e){
                    document.onmouseup=null;
                    document.onmousemove = null;
                    video.play();
                    videoPlayer.setPlayProgress( e.pageX );
                    videoPlayer.trackPlayProgress();
                }
            }, true)
        },
        setPlayProgress:function(clickX){
            var newPercent = Math.max( 0, Math.min(1, (clickX - this.findPosX(progressHolder)) / progressHolder.offsetWidth) ); 
            video.currentTime = newPercent * video.duration; 
            playProgressBar.style.width = newPercent * (progressHolder.offsetWidth) + "px";
        },
        findPosX:function(progressHolder){
            var curleft = progressHolder.offsetLeft; 
            while( progressHolder = progressHolder.offsetParent ) { 
                curleft += progressHolder.offsetLeft; } 
            return curleft;
        }
    };
    videoPlayer.init();

}(this, document))