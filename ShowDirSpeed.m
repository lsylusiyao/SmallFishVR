function result = ShowDirSpeed(path)
% ��VR�ֱ����ݽ���Ϊ0123֮��ĺ���

    close all;

    %% ������ȡ����

    data = load(path);
    divisionPoint = [15,25,40,55];
    dataColor = data(:,1);
    dataSpeed = data(:,3);
    dataDirection = ones(length(dataSpeed),1);
    dataLR = data(:,4);
    dataFB = data(:,5);
    t = (1:length(data)) .* 25;
        
    %% ����ת������

    dataColor(dataColor >= divisionPoint(1)) = -1;
    dataColor(dataColor <= -divisionPoint(1)) = 1;
    dataColor(dataColor < divisionPoint(1) & dataColor > -divisionPoint(1)) = 0;
    
    dataSpeed(dataSpeed >= -divisionPoint(1)) = 0;
    dataSpeed(dataSpeed < -divisionPoint(1) & dataSpeed >= -divisionPoint(2)) = 1;
    dataSpeed(dataSpeed < -divisionPoint(2) & dataSpeed >= -divisionPoint(3)) = 2;
    dataSpeed(dataSpeed < -divisionPoint(3) & dataSpeed >= -divisionPoint(4)) = 3;
    dataSpeed(dataSpeed < -divisionPoint(4)) = 4;
    
    % ֹͣ��0�� ǰ����1�� ����2�� ����3
    dataDirection(abs(dataLR) < 10 & abs(dataFB) < 10) = 0;
    dataDirection(~(abs(dataLR) < 10 & abs(dataFB) < 10) & abs(dataLR) >= abs(dataFB)) = 2; % �ҵ��������Ҳ���
    dataDirection(dataDirection == 2 & dataLR > 0) = 3;
    dataDirection(dataDirection == 1 & dataFB <= 0) = 0;
    % ʣ�µĶ���ǰ����

    % ��direction������0�Ĳ��ֶ�Ӧ��dataSpeed���0����Ϊû������
    dataSpeed(dataDirection == 0) = 0;
    
    % �ٰѷ��������е�0���ǰ��ķ����÷���ͼ���ĺÿ����˶�ֻ���ٶ�ͼ������
    for j = 1:length(dataDirection) - 1
        tempDirection = dataDirection(j);
        if(dataDirection(j + 1) == 0) 
            dataDirection(j + 1) = tempDirection;
        end
    end


    %% ����ǰ��ͼ�񲿷�
    figure;
    p = plot(t, data(:,1),'k','linewidth', 1.5); %dataColor
    p.Parent.XAxis.FontSize = 16; %����X�������ֺ�
    p.Parent.YAxis.FontSize = 16; %����y�������ֺ�
    xlabel('ʱ�� / ms');
    ylabel('�Ƕ�');
    title('��ɫͨ��ԭʼ����', 'FontSize', 18);
    axis([-Inf Inf min(data(:,1)) - 1 max(data(:,1)) + 1]);
    grid on;

    figure;
    subplot(1,3,1);
    p = plot(t, data(:,3),'k','linewidth', 1.5); %dataSpeed
    p.Parent.XAxis.FontSize = 16; %����X�������ֺ�
    p.Parent.YAxis.FontSize = 16; %����y�������ֺ�
    xlabel('ʱ�� / ms');
    ylabel('�Ƕ�');
    title('�ٶ�ͨ��ԭʼ����', 'FontSize', 18);
    axis([-Inf Inf min(data(:,3)) - 1 max(data(:,3)) + 1]);
    grid on;

    subplot(1,3,2);
    p = plot(t, data(:,4),'k','linewidth', 1.5); %dataLR
    p.Parent.XAxis.FontSize = 16; %����X�������ֺ�
    p.Parent.YAxis.FontSize = 16; %����y�������ֺ�
    xlabel('ʱ�� / ms');
    ylabel('����λ��X����');
    title('����λ��X����ԭʼ����', 'FontSize', 18);
    axis([-Inf Inf min(data(:,4)) - 1 max(data(:,4)) + 1]);
    grid on;

    subplot(1,3,3);
    p = plot(t, data(:,5),'k','linewidth', 1.5); %dataFB
    p.Parent.XAxis.FontSize = 16; %����X�������ֺ�
    p.Parent.YAxis.FontSize = 16; %����y�������ֺ�
    xlabel('ʱ�� / ms');
    ylabel('����λ��Y����');
    title('����λ��Y����ԭʼ����', 'FontSize', 18);
    axis([-Inf Inf min(data(:,5)) - 1 max(data(:,5)) + 1]);
    grid on;

    %% ���˺��ͼ�񲿷�

    figure;
    p = plot(t, dataColor,'k','linewidth', 1.5);
    p.Parent.XAxis.FontSize = 16; %����X�������ֺ�
    p.Parent.YAxis.FontSize = 16; %����y�������ֺ�
    xlabel('ʱ�� / ms');
    ylabel('��ɫ�л�����');
    title('��ɫ�л�ͼ��', 'FontSize', 18);
    axis([-Inf Inf min(dataColor) - 1 max(dataColor) + 1]);
    grid on;
    
    figure;
    subplot(1,2,1);
    p = plot(t, dataSpeed,'k','linewidth', 1.5);
    p.Parent.XAxis.FontSize = 16; %����X�������ֺ�
    p.Parent.YAxis.FontSize = 16; %����y�������ֺ�
    xlabel('ʱ�� / ms');
    ylabel('�ٶȵ�λ');
    title('�ٶ��л�ͼ��', 'FontSize', 18);
    axis([-Inf Inf min(dataSpeed) - 1 max(dataSpeed) + 1]);
    grid on;
    
    subplot(1,2,2);
    p = plot(t, dataDirection,'k','linewidth', 1.5);
    p.Parent.XAxis.FontSize = 16; %����X�������ֺ�
    p.Parent.YAxis.FontSize = 16; %����y�������ֺ�
    xlabel('ʱ�� / ms');
    ylabel('�����л���λ');
    title('�����л�ͼ��', 'FontSize', 18);
    axis([-Inf Inf min(dataDirection) - 1 max(dataDirection) + 1]);
    grid on;
    
    result = true;
end