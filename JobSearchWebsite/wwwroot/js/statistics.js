document.addEventListener('DOMContentLoaded', function () {
    // Biểu đồ cột cho người dùng
    var userCtx = document.getElementById('userBarChart').getContext('2d');
    new Chart(userCtx, {
        type: 'bar',
        data: {
            labels: ['Admin', 'Nhà tuyển dụng', 'Người tìm việc'],
            datasets: [{
                label: 'Người dùng theo vai trò',
                data: [@ViewBag.AdminCount, @ViewBag.EmployerCount, @ViewBag.JobSeekerCount],
                backgroundColor: ['#007bff', '#ffc107', '#dc3545'],
                borderWidth: 1
            }]
        },
        options: {
            animation: {
                duration: 1500,
                easing: 'easeInOutQuart'
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Số lượng'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Vai trò'
                    }
                }
            },
            plugins: {
                legend: {
                    display: false
                }
            }
        }
    });

    // Biểu đồ tròn cho công việc
    var jobCtx = document.getElementById('jobPieChart').getContext('2d');
    new Chart(jobCtx, {
        type: 'pie',
        data: {
            labels: ['Mới tạo', 'Đã duyệt', 'Đã đóng'],
            datasets: [{
                label: 'Công việc theo trạng thái',
                data: [@ViewBag.NewJobCount, @ViewBag.ApprovedJobCount, @ViewBag.ClosedJobCount],
                backgroundColor: ['#6c757d', '#28a745', '#343a40'],
                borderWidth: 1
            }]
        },
        options: {
            animation: {
                duration: 1500,
                easing: 'easeInOutQuart'
            },
            plugins: {
                legend: {
                    position: 'bottom'
                }
            }
        }
    });

    // Biểu đồ tròn cho đơn ứng tuyển
    var applicationCtx = document.getElementById('applicationPieChart').getContext('2d');
    new Chart(applicationCtx, {
        type: 'pie',
        data: {
            labels: ['Chờ xử lý', 'Được duyệt', 'Bị từ chối'],
            datasets: [{
                label: 'Đơn ứng tuyển theo trạng thái',
                data: [@ViewBag.PendingApplicationCount, @ViewBag.ApprovedApplicationCount, @ViewBag.RejectedApplicationCount],
                backgroundColor: ['#ffc107', '#28a745', '#dc3545'],
                borderWidth: 1
            }]
        },
        options: {
            animation: {
                duration: 1500,
                easing: 'easeInOutQuart'
            },
            plugins: {
                legend: {
                    position: 'bottom'
                }
            }
        }
    });

    // Biểu đồ đường cho xu hướng công việc theo tháng
    var jobTrendCtx = document.getElementById('jobTrendLineChart').getContext('2d');
    new Chart(jobTrendCtx, {
        type: 'line',
        data: {
            labels: ['Th1', 'Th2', 'Th3', 'Th4', 'Th5', 'Th6', 'Th7', 'Th8', 'Th9', 'Th10', 'Th11', 'Th12'],
            datasets: [
                {
                    label: 'Công việc mới tạo',
                    data: @Html.Raw(Json.Serialize(ViewBag.JobTrendNew)),
                    borderColor: '#6c757d',
                    fill: false,
                    tension: 0.3
                },
                {
                    label: 'Công việc đã duyệt',
                    data: @Html.Raw(Json.Serialize(ViewBag.JobTrendApproved)),
                    borderColor: '#28a745',
                    fill: false,
                    tension: 0.3
                }
            ]
        },
        options: {
            animation: {
                duration: 1500,
                easing: 'easeInOutQuart'
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Số lượng công việc'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Tháng'
                    }
                }
            }
        }
    });
});