function result = ShowDirSpeed(path)
% ��VR�ֱ����ݽ���Ϊ0123֮��ĺ���

    %% ������ȡ����

    data = load(path);
    divisionPoint = [15,25,40,55];
    dataColor = data(:,4);
    dataSpeed = data(:,6);
    dataDirection = ones(length(dataSpeed),1);
    dataLR = data(:,7);
    dataFB = data(:,8);
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
    
    %% ����ǰ��ͼ�񲿷�
    figure;
    plot(t, data(:,4)); %dataColor
    xlabel('ʱ�� / ms');
    ylabel('�Ƕ�');
    title('��ɫͨ��ԭʼ����');
    grid on;

    subplot(1,3,1);
    plot(t, data(:,6)); %dataSpeed
    xlabel('ʱ�� / ms');
    ylabel('�Ƕ�');
    title('�ٶ�ͨ��ԭʼ����');
    grid on;

    subplot(1,3,2);
    plot(t, data(:,7)); %dataLR
    xlabel('ʱ�� / ms');
    ylabel('������');
    title('�����̵ĺ�����ԭʼ����');
    grid on;

    subplot(1,3,3);
    plot(t, data(:,8)); %dataFB
    xlabel('ʱ�� / ms');
    ylabel('������');
    title('������������ԭʼ����');
    grid on;

    %% ���˺��ͼ�񲿷�

    figure;
    plot(t, dataColor);
    xlabel('ʱ�� / ms');
    ylabel('��ɫ�л�����');
    title('��ɫ�л�ͼ��');
    grid on;
    
    subplot(1,2,1);
    plot(t, dataSpeed);
    xlabel('ʱ�� / ms');
    ylabel('�ٶȵ�λ');
    title('�ٶ��л�ͼ��');
    grid on;
    
    subplot(1,2,2);
    plot(t, dataDirection);
    xlabel('ʱ�� / ms');
    ylabel('�����л���λ');
    title('�����л�ͼ��');
    grid on;
    
    result = true;
end