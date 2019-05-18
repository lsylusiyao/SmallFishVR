function result = ShowDirSpeed(path)
% 将VR手柄数据解析为0123之类的函数

    %% 数据提取部分

    data = load(path);
    divisionPoint = [15,25,40,55];
    dataColor = data(:,1);
    dataSpeed = data(:,3);
    dataDirection = ones(length(dataSpeed),1);
    dataLR = data(:,4);
    dataFB = data(:,5);
    t = (1:length(data)) .* 25;
        
    %% 数据转换部分

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

    % 把direction里面是0的部分对应的dataSpeed变成0，因为没有意义
    dataSpeed(dataDirection == 0) = 0;
    
    %% 过滤前的图像部分
    figure;
    plot(t, data(:,1)); %dataColor
    xlabel('时间 / ms');
    ylabel('角度');
    title('颜色通道原始数据');
    axis tight;
    grid on;

    subplot(1,3,1);
    plot(t, data(:,3)); %dataSpeed
    xlabel('时间 / ms');
    ylabel('角度');
    title('速度通道原始数据');
    axis tight;
    grid on;

    subplot(1,3,2);
    plot(t, data(:,4)); %dataLR
    xlabel('时间 / ms');
    ylabel('横坐标');
    title('触摸盘的横坐标原始数据');
    axis tight;
    grid on;

    subplot(1,3,3);
    plot(t, data(:,5)); %dataFB
    xlabel('时间 / ms');
    ylabel('纵坐标');
    title('触摸盘纵坐标原始数据');
    axis tight;
    grid on;

    %% 过滤后的图像部分

    figure;
    plot(t, dataColor);
    xlabel('时间 / ms');
    ylabel('颜色切换方向');
    title('颜色切换图像');
    axis tight;
    grid on;
    
    subplot(1,2,1);
    plot(t, dataSpeed);
    xlabel('时间 / ms');
    ylabel('速度档位');
    title('速度切换图像');
    axis tight;
    grid on;
    
    subplot(1,2,2);
    plot(t, dataDirection);
    xlabel('时间 / ms');
    ylabel('方向切换档位');
    title('方向切换图像');
    axis tight;
    grid on;
    
    result = true;
end