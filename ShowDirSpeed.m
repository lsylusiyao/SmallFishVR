function result = ShowDirSpeed(path)
% 将VR手柄数据解析为0123之类的函数
    data = load(path);
    divisionPoint = [15,25,40,55];
    dataColor = data(:,4);
    dataSpeed = data(:,6);
    dataDirection = ones(length(dataSpeed),1);
    dataLR = data(:,7);
    dataFB = data(:,8);
    
    dataColor(dataColor >= divisionPoint(1)) = -1;
    dataColor(dataColor <= -divisionPoint(1)) = 1;
    dataColor(dataColor < divisionPoint(1) & dataColor > -divisionPoint(1)) = 0;
    
    dataSpeed(dataSpeed >= -divisionPoint(1)) = 0;
    dataSpeed(dataSpeed < -divisionPoint(1) & dataSpeed >= -divisionPoint(2)) = 1;
    dataSpeed(dataSpeed < -divisionPoint(2) & dataSpeed >= -divisionPoint(3)) = 2;
    dataSpeed(dataSpeed < -divisionPoint(3) & dataSpeed >= -divisionPoint(4)) = 3;
    dataSpeed(dataSpeed < -divisionPoint(4)) = 4;
    
    % 停止是0， 前进是1， 左是2， 右是3
    dataDirection(abs(dataLR) < 10 & abs(dataFB) < 10) = 0;
    dataDirection(~(abs(dataLR) < 10 & abs(dataFB) < 10) & abs(dataLR) >= abs(dataFB)) = 2; % 找到所有左右部分
    dataDirection(dataDirection == 2 & dataLR > 0) = 3;
    dataDirection(dataDirection == 1 & dataFB <= 0) = 0;
    % 剩下的都是前进了
    
    figure;
    plot(dataColor);
    xlabel('时间');
    ylabel('颜色切换方向');
    title('颜色切换图像');
    grid on;
    
    figure;
    plot(dataSpeed);
    xlabel('速度');
    ylabel('速度档位');
    title('速度切换图像');
    grid on;
    
    figure;
    plot(dataDirection);
    xlabel('方向');
    ylabel('方向切换档位');
    title('方向切换图像');
    grid on;
    
    result = true;
end