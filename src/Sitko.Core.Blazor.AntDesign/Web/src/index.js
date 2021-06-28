import './index.css';

window.SitkoCoreBlazorAntDesign = {
    FileUpload: {
        init: function (fileInput, button, componentInstance) {
            button.onclick = function () {
                fileInput.click();
            };
            fileInput.addEventListener('change', function handleInputFileChange(event) {
                componentInstance.invokeMethodAsync('NotifyChange').then(function () {
                    //reset file value ,otherwise, the same filename will not be trigger change event again
                    fileInput.value = '';
                }, function (err) {
                    //reset file value ,otherwise, the same filename will not be trigger change event again
                    fileInput.value = '';
                    throw new Error(err);
                });
            });
        }
    }
}
