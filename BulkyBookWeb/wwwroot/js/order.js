var dataTable;

$(document).ready(function () {
    var url = window.location.search;
    if (url.includes("inprocess")) {

        loadDataTable("inprocess");

    } else {
        if (url.includes("completed")) {

            loadDataTable("completed");

        } else {

            if (url.includes("approved")) {

                loadDataTable("approved");

            } else {

                if (url.includes("pending")) {

                    loadDataTable("pending");

                } else {

                    loadDataTable("all");
                }

                
            }
        }
    }
    
}); 

function loadDataTable() {
    dataTable = $('#mytable').DataTable({
        "ajax": {
            "url": "/Admin/Order/GetAll?status=" + status
        
        },
        "columns": [
            { "data": "id", "width": "15%" },
            { "data": "name", "width": "15%" },
            { "data": "phoneNumber", "width": "15%" },
            { "data": "applicationUser.email", "width": "15%" },
            { "data": "orderStatus", "width": "15%" },
            { "data": "orderTotal", "width": "15%" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="w-75 btn-group" role="group">
                            <a href="/Admin/Order/Details?orderId=${data}"
                                class="btn btn-primary mx-2"> <i class="bi bi-pencil-square"></i>Edit
                            </a>
                            <a onClick=Delete('/Admin/Order/Delete/${data}')  class="btn btn-danger mx-2">
                                <i class="bi bi-trash"></i>Delete
                            </a>
                        </div>
                    `
                },
                "width": "15%"
            },
            /*{ "data": "coverType.name", "width": "15%" },*/
            
        ]
    })
}
function Delete(url) {
    Swal.fire({
        title: 'Are you sure?',
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    }
                    else {
                        toastr.error(data.message);
                    }
                }
            })
        }
    })
}