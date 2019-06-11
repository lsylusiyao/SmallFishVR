function result = ShowDirSpeed(path)
% 将VR手柄数据解析为0123之类的函数

    close all;

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
    
    % 再把方向中所有的0变成前面的方向，让方向图像变的好看，运动只由速度图像限制
    for j = 1:length(dataDirection) - 1
        tempDirection = dataDirection(j);
        if(dataDirection(j + 1) == 0) 
            dataDirection(j + 1) = tempDirection;
        end
    end


    %% 过滤前的图像部分
    figure;
    p = plot(t, data(:,1),'k','linewidth', 1.5); %dataColor
    p.Parent.XAxis.FontSize = 16; %更改X轴数据字号
    p.Parent.YAxis.FontSize = 16; %更改y轴数据字号
    xlabel('时间 / ms');
    ylabel('角度');
    title('颜色通道原始数据', 'FontSize', 18);
    axis([-Inf Inf min(data(:,1)) - 1 max(data(:,1)) + 1]);
    grid on;

    figure;
    subplot(1,3,1);
    p = plot(t, data(:,3),'k','linewidth', 1.5); %dataSpeed
    p.Parent.XAxis.FontSize = 16; %更改X轴数据字号
    p.Parent.YAxis.FontSize = 16; %更改y轴数据字号
    xlabel('时间 / ms');
    ylabel('角度');
    title('速度通道原始数据', 'FontSize', 18);
    axis([-Inf Inf min(data(:,3)) - 1 max(data(:,3)) + 1]);
    grid on;

    subplot(1,3,2);
    p = plot(t, data(:,4),'k','linewidth', 1.5); %dataLR
    p.Parent.XAxis.FontSize = 16; %更改X轴数据字号
    p.Parent.YAxis.FontSize = 16; %更改y轴数据字号
    xlabel('时间 / ms');
    ylabel('触摸位置X坐标');
    title('触摸位置X坐标原始数据', 'FontSize', 18);
    axis([-Inf Inf min(data(:,4)) - 1 max(data(:,4)) + 1]);
    grid on;

    subplot(1,3,3);
    p = plot(t, data(:,5),'k','linewidth', 1.5); %dataFB
    p.Parent.XAxis.FontSize = 16; %更改X轴数据字号
    p.Parent.YAxis.FontSize = 16; %更改y轴数据字号
    xlabel('时间 / ms');
    ylabel('触摸位置Y坐标');
    title('触摸位置Y坐标原始数据', 'FontSize', 18);
    axis([-Inf Inf min(data(:,5)) - 1 max(data(:,5)) + 1]);
    grid on;

    %% 过滤后的图像部分

    figure;
    p = plot(t, dataColor,'k','linewidth', 1.5);
    p.Parent.XAxis.FontSize = 16; %更改X轴数据字号
    p.Parent.YAxis.FontSize = 16; %更改y轴数据字号
    xlabel('时间 / ms');
    ylabel('颜色切换方向');
    title('颜色切换图像', 'FontSize', 18);
    axis([-Inf Inf min(dataColor) - 1 max(dataColor) + 1]);
    grid on;
    
    figure;
    subplot(1,2,1);
    p = plot(t, dataSpeed,'k','linewidth', 1.5);
    p.Parent.XAxis.FontSize = 16; %更改X轴数据字号
    p.Parent.YAxis.FontSize = 16; %更改y轴数据字号
    xlabel('时间 / ms');
    ylabel('速度档位');
    title('速度切换图像', 'FontSize', 18);
    axis([-Inf Inf min(dataSpeed) - 1 max(dataSpeed) + 1]);
    grid on;
    
    subplot(1,2,2);
    p = plot(t, dataDirection,'k','linewidth', 1.5);
    p.Parent.XAxis.FontSize = 16; %更改X轴数据字号
    p.Parent.YAxis.FontSize = 16; %更改y轴数据字号
    xlabel('时间 / ms');
    ylabel('方向切换档位');
    title('方向切换图像', 'FontSize', 18);
    axis([-Inf Inf min(dataDirection) - 1 max(dataDirection) + 1]);
    grid on;
    
    result = true;
end