import './index.css';

window.SitkoCoreBlazorAntDesign = {
    FileUpload: {
        init: function (elem, componentInstance) {
            elem.addEventListener('change', function handleInputFileChange(event) {
                componentInstance.invokeMethodAsync('NotifyChange').then(function () {
                    //reset file value ,otherwise, the same filename will not be trigger change event again
                    elem.value = '';
                }, function (err) {
                    //reset file value ,otherwise, the same filename will not be trigger change event again
                    elem.value = '';
                    throw new Error(err);
                });
            });
        },
        open: function (elem) {
            elem.click();
        }
    }
}
